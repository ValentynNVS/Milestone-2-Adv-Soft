
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FDMS.GroundTerminal.Models;
using FDMS.GroundTerminal.Services;
using FDMS.GroundTerminal.Data;

namespace FDMS.GroundTerminal.Services
{
    public class RealTelemetryService : IDatabaseService
    {
        private readonly List<TelemetryRecord> _telemetryData = new List<TelemetryRecord>();
        private readonly List<InvalidPacket> _invalidPackets = new List<InvalidPacket>();
        private readonly HashSet<string> _tailNumbers = new HashSet<string>();
        private readonly TcpListener _listener;
        private volatile bool _running;

        //databse
        private readonly IDatabaseService _databaseService;

        // Configure the port to match ATS (default 8080)
        public RealTelemetryService(IDatabaseService databaseService, int port = 8080)
        {
            _databaseService = databaseService;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _running = true;
            Console.WriteLine("RealTelemetryService: Listening for ATS packets on port " + port);
            // Fire and forget accept loop (no async lambdas required)
            Task.Run(new Func<Task>(AcceptLoopAsync));
        }

        private async Task AcceptLoopAsync()
        {
            while (_running)
            {
                TcpClient client = null;
                try
                {
                    client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    // Handle client in background
                    Task.Run(() => HandleClientAsync(client));
                }
                catch (ObjectDisposedException)
                {
                    // Listener stopped—exit
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("AcceptLoop error: " + ex.Message);
                    if (client != null) client.Close();
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("Client connected from " + client.Client.RemoteEndPoint);
            try
            {
                using (var stream = client.GetStream())
                {
                    var buffer = new byte[4096];
                    int bytesRead;
                    var sb = new StringBuilder(); // accumulate data across reads

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        var chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        sb.Append(chunk);

                        // Packets are delimited by newline per ATS SocketClient
                        // Process complete lines and keep the remainder in sb
                        int newlineIndex;
                        while ((newlineIndex = sb.ToString().IndexOf('\n')) >= 0)
                        {
                            string all = sb.ToString();
                            string line = all.Substring(0, newlineIndex).TrimEnd('\r');
                            string remainder = all.Substring(newlineIndex + 1);
                            sb.Clear();
                            sb.Append(remainder);

                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                ProcessPacket(line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleClient error: " + ex.Message);
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
        private void ProcessPacket(string packet)
        {
            try
            {
                // Expected format: TailNumber|SeqNum|Timestamp,AccelX,AccelY,AccelZ,Weight,Alt,Pitch,Bank|Checksum
                var parts = packet.Split('|');
                if (parts.Length != 4)
                {
                    var invalidPacket = new InvalidPacket
                    {
                        ReceivedAt = DateTime.Now,
                        TailNumber = "UNKNOWN",
                        SequenceNumber = -1,
                        Reason = "Malformed packet (parts != 4)",
                        RawPacket = packet
                    };
                    _invalidPackets.Add(invalidPacket);
                    _databaseService.StoreInvalidPacket(invalidPacket);
                    return;
                }

                string tailNumber = parts[0];
                int seqNum;
                if (!int.TryParse(parts[1], out seqNum))
                {
                    var invalidPacket = new InvalidPacket
                    {
                        ReceivedAt = DateTime.Now,
                        TailNumber = tailNumber,
                        SequenceNumber = -1,
                        Reason = "Invalid sequence number",
                        RawPacket = packet
                    };
                    _invalidPackets.Add(invalidPacket);
                    _databaseService.StoreInvalidPacket(invalidPacket);
                    return;
                }

                string telemetryCsv = parts[2];
                int checksum;
                if (!int.TryParse(parts[3], out checksum))
                {
                    var invalidPacket = new InvalidPacket
                    {
                        ReceivedAt = DateTime.Now,
                        TailNumber = tailNumber,
                        SequenceNumber = seqNum,
                        Reason = "Invalid checksum (non-integer)",
                        RawPacket = packet
                    };
                    _invalidPackets.Add(invalidPacket);
                    _databaseService.StoreInvalidPacket(invalidPacket);
                    return;
                }

                var values = telemetryCsv.Split(',');
                if (values.Length != 8)
                {
                    var invalidPacket = new InvalidPacket
                    {
                        ReceivedAt = DateTime.Now,
                        TailNumber = tailNumber,
                        SequenceNumber = seqNum,
                        Reason = "Malformed telemetry CSV (values != 8)",
                        RawPacket = packet
                    };
                    _invalidPackets.Add(invalidPacket);
                    _databaseService.StoreInvalidPacket(invalidPacket);
                    return;
                }

                // Parse values
                DateTime ts;
                double ax, ay, az, wt, alt, pitch, bank;

                if (!DateTime.TryParse(values[0], out ts) ||
                    !double.TryParse(values[1], out ax) ||
                    !double.TryParse(values[2], out ay) ||
                    !double.TryParse(values[3], out az) ||
                    !double.TryParse(values[4], out wt) ||
                    !double.TryParse(values[5], out alt) ||
                    !double.TryParse(values[6], out pitch) ||
                    !double.TryParse(values[7], out bank))
                {
                    var invalidPacket = new InvalidPacket
                    {
                        ReceivedAt = DateTime.Now,
                        TailNumber = tailNumber,
                        SequenceNumber = seqNum,
                        Reason = "Telemetry parse failure",
                        RawPacket = packet
                    };
                    _invalidPackets.Add(invalidPacket);
                    _databaseService.StoreInvalidPacket(invalidPacket);
                    return;
                }

                var record = new TelemetryRecord
                {
                    Timestamp = ts,
                    TailNumber = tailNumber,
                    AccelX = ax,
                    AccelY = ay,
                    AccelZ = az,
                    Weight = wt,
                    Altitude = alt,
                    Pitch = pitch,
                    Bank = bank,
                    SequenceNumber = seqNum,
                    IsFromRealTime = true
                };

                // Validate checksum = int((Altitude + Pitch + Bank) / 3)
                int calculatedChecksum = (int)((record.Altitude + record.Pitch + record.Bank) / 3.0);
                if (calculatedChecksum != checksum)
                {
                    var invalidPacket = new InvalidPacket
                    {
                        ReceivedAt = DateTime.Now,
                        TailNumber = tailNumber,
                        SequenceNumber = seqNum,
                        Reason = "Checksum mismatch",
                        RawPacket = packet
                    };
                    _invalidPackets.Add(invalidPacket);
                    // REQ-FN-020: Store invalid packet to database
                    _databaseService.StoreInvalidPacket(invalidPacket);
                }
                else
                {
                    _telemetryData.Add(record);
                    _tailNumbers.Add(tailNumber);

                    // REQ-FN-030b: Store valid telemetry in database
                    _databaseService.StoreTelemetry(record);

                    Console.WriteLine($"Valid packet from {tailNumber}, Seq: {seqNum}, Alt: {record.Altitude:F1}");
                }
            }
            catch (Exception ex)
            {
                var invalidPacket = new InvalidPacket
                {
                    ReceivedAt = DateTime.Now,
                    TailNumber = "UNKNOWN",
                    SequenceNumber = -1,
                    Reason = "Exception: " + ex.Message,
                    RawPacket = packet
                };
                _invalidPackets.Add(invalidPacket);
                _databaseService.StoreInvalidPacket(invalidPacket);
            }
    }
        // IDatabaseService implementation methods:
        public IList<string> GetTailNumbers()
        {
            return new List<string>(_tailNumbers);
        }

        public TelemetryRecord GetLatestTelemetry(string tailNumber)
        {
            for (int i = _telemetryData.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_telemetryData[i].TailNumber, tailNumber, StringComparison.OrdinalIgnoreCase))
                    return _telemetryData[i];
            }
            return null;
        }
        public IList<TelemetryRecord> SearchTelemetry(string tailNumber, DateTime from, DateTime to)
        {
            // Search from DATABASE, not in-memory
            return _databaseService.SearchTelemetry(tailNumber, from, to);
        }

        public IList<InvalidPacket> SearchInvalidPackets(string tailNumber, DateTime from, DateTime to)
        {
            // Search from DATABASE, not in-memory
            return _databaseService.SearchInvalidPackets(tailNumber, from, to);
        }

        public DatabaseStatus TestConnection()
        {
            return _databaseService.TestConnection();
        }

        public bool StoreTelemetry(TelemetryRecord record)
        {
            return _databaseService.StoreTelemetry(record);
        }

        public bool StoreInvalidPacket(InvalidPacket packet)
        {
            return _databaseService.StoreInvalidPacket(packet);
        }

        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
        }
    }
}
