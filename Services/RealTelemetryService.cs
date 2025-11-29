/*
 File          : RealTelemetryService.cs
 Project       : SENG3020 - Term Project
 Programmer    : Bhawanjeet Kaur Gill
 File Version  : 11/24/2025
 Description   : Implements a real-time telemetry service that listens for ATS packets over TCP, 
                 parses incoming data, validates packet structure and content, and stores valid telemetry 
                 records or invalid packets in the database. 
*/
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
        /*
          Function: RealTelemetryService()
          Description: Configure the port to match ATS (default 8080).
          Parameters: IDatabaseService databaseService, int port = 8080
          Return Values:  
        */
        public RealTelemetryService(IDatabaseService databaseService, int port = 8080)
        {
            _databaseService = databaseService;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _running = true;
            Console.WriteLine("RealTelemetryService: Listening for ATS packets on port " + port);
            
            Task.Run(new Func<Task>(AcceptLoopAsync));
        }

        /*
          Function: AcceptLoopAsync()
          Description: Continuously listens for incoming TCP client connections and starts a handler task for each client.
          Parameters: None
          Return Values: Task representing the asynchronous accept loop.
        */
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

        /*
          Function: HandleClientAsync()
          Description: Handles communication with a connected TCP client by reading data from the stream, 
                       splitting packets by newline, and passing each packet to ProcessPacket().
          Parameters: TcpClient client.
          Return Values: Task representing the asynchronous client handling operation.
        */
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

        /*
          Function: ProcessPacket()
          Description: Parses and validates a telemetry packet. Stores valid telemetry data or logs invalid packets 
                       with reasons into the database.
          Parameters: string packet.
          Return Values: None
        */
        private void ProcessPacket(string packet)
        {
            try
            {
                // Expecting the format: TailNumber|SeqNum|Timestamp,AccelX,AccelY,AccelZ,Weight,Alt,Pitch,Bank|Checksum
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

        /*
          Function: GetTailNumbers()
          Description: Retrieves a list of all unique tail numbers currently stored in memory.
          Parameters: None
          Return Values: IList<string>.
        */

        public IList<string> GetTailNumbers()
        {
            return new List<string>(_tailNumbers);
        }

        /*
          Function: GetLatestTelemetry()
          Description: Returns the most recent telemetry record for the specified tail number.
          Parameters: string tailNumber.
          Return Values: TelemetryRecord.
        */
        public TelemetryRecord GetLatestTelemetry(string tailNumber)
        {
            for (int i = _telemetryData.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_telemetryData[i].TailNumber, tailNumber, StringComparison.OrdinalIgnoreCase))
                    return _telemetryData[i];
            }
            return null;
        }

        /*
          Function: SearchTelemetry()
          Description: Searches telemetry records for the specified tail number within the given date range.
          Parameters: string tailNumber.
                      DateTime from.
                      DateTime to.
          Return Values: IList<TelemetryRecord>.
        */

        public IList<TelemetryRecord> SearchTelemetry(string tailNumber, DateTime from, DateTime to)
        {
            // Search from DATABASE, not in-memory
            return _databaseService.SearchTelemetry(tailNumber, from, to);
        }

        /*
          Function: SearchInvalidPackets()
          Description: Searches invalid packet records for the specified tail number within the given date range.
          Parameters: string tailNumber.
                      DateTime from.
                      DateTime to.
          Return Values: IList<InvalidPacket> - A list of invalid packets matching the criteria.
        */
        public IList<InvalidPacket> SearchInvalidPackets(string tailNumber, DateTime from, DateTime to)
        {
            return _databaseService.SearchInvalidPackets(tailNumber, from, to);
        }

        /*
          Function: TestConnection()
          Description: Tests the connection to the database service.
          Parameters: None
          Return Values: DatabaseStatus
        */
        public DatabaseStatus TestConnection()
        {
            return _databaseService.TestConnection();
        }

        /*
          Function: StoreTelemetry()
          Description: Stores a valid telemetry record in the database.
          Parameters: TelemetryRecord record
          Return Values: bool
        */
        public bool StoreTelemetry(TelemetryRecord record)
        {
            return _databaseService.StoreTelemetry(record);
        }

        /*
          Function: StoreInvalidPacket()
          Description: Stores an invalid packet record in the database.
          Parameters: InvalidPacket packet.
          Return Values: bool
        */
        public bool StoreInvalidPacket(InvalidPacket packet)
        {
            return _databaseService.StoreInvalidPacket(packet);
        }

        /*
          Function: Stop()
          Description: Stops the telemetry service by halting the TCP listener and ending the accept loop.
          Parameters: None
          Return Values: None
        */
        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
        }
    }
}
