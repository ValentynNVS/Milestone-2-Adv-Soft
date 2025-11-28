using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using FDMS.GroundTerminal.Models;

namespace FDMS.GroundTerminal.Services
{
    public class SqlServerDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public SqlServerDatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DatabaseStatus TestConnection()
        {
            var status = new DatabaseStatus
            {
                ServerName = "localhost\\SQLEXPRESS",
                DatabaseName = "FDMS",
                IsConnected = false,
                StatusMessage = "Not tested"
            };

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    status.IsConnected = true;
                    status.StatusMessage = "Connected successfully";
                }
            }
            catch (Exception ex)
            {
                status.StatusMessage = "Error: " + ex.Message;
            }

            return status;
        }

        public bool StoreTelemetry(TelemetryRecord record)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string gforceSql = @"INSERT INTO GForceParameters 
                        (Tail_number, Packet_sequence, Aircraft_timestamp, Accel_x, Accel_y, Accel_z, Weight)
                        VALUES (@Tail, @Seq, @Time, @AccelX, @AccelY, @AccelZ, @Weight)";

                    using (var cmd = new SqlCommand(gforceSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Tail", record.TailNumber);
                        cmd.Parameters.AddWithValue("@Seq", record.SequenceNumber);
                        cmd.Parameters.AddWithValue("@Time", record.Timestamp);
                        cmd.Parameters.AddWithValue("@AccelX", record.AccelX);
                        cmd.Parameters.AddWithValue("@AccelY", record.AccelY);
                        cmd.Parameters.AddWithValue("@AccelZ", record.AccelZ);
                        cmd.Parameters.AddWithValue("@Weight", record.Weight);
                        cmd.ExecuteNonQuery();
                    }

                    string attitudeSql = @"INSERT INTO AttitudeParameters 
                        (Tail_number, Packet_sequence, Aircraft_timestamp, Altitude, Pitch, Bank, Weight)
                        VALUES (@Tail, @Seq, @Time, @Altitude, @Pitch, @Bank, @Weight)";

                    using (var cmd = new SqlCommand(attitudeSql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Tail", record.TailNumber);
                        cmd.Parameters.AddWithValue("@Seq", record.SequenceNumber);
                        cmd.Parameters.AddWithValue("@Time", record.Timestamp);
                        cmd.Parameters.AddWithValue("@Altitude", record.Altitude);
                        cmd.Parameters.AddWithValue("@Pitch", record.Pitch);
                        cmd.Parameters.AddWithValue("@Bank", record.Bank);
                        cmd.Parameters.AddWithValue("@Weight", record.Weight);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool StoreInvalidPacket(InvalidPacket packet)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"INSERT INTO ErrorLog (Packet_sequence, Tail_number, Raw_packet)
                        VALUES (@Seq, @Tail, @Raw)";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Seq", packet.SequenceNumber);
                        cmd.Parameters.AddWithValue("@Tail", packet.TailNumber);
                        cmd.Parameters.AddWithValue("@Raw", packet.RawPacket);
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IList<string> GetTailNumbers()
        {
            var tailNumbers = new List<string>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT DISTINCT Tail_number FROM GForceParameters ORDER BY Tail_number";

                    using (var cmd = new SqlCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tailNumbers.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }

            return tailNumbers;
        }

        public IList<TelemetryRecord> SearchTelemetry(string tailNumber, DateTime from, DateTime to)
        {
            var records = new List<TelemetryRecord>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"SELECT g.Tail_number, g.Packet_sequence, g.Aircraft_timestamp,
                            g.Accel_x, g.Accel_y, g.Accel_z, g.Weight,
                            a.Altitude, a.Pitch, a.Bank
                        FROM GForceParameters g
                        INNER JOIN AttitudeParameters a 
                            ON g.Tail_number = a.Tail_number AND g.Packet_sequence = a.Packet_sequence
                        WHERE g.Tail_number = @Tail AND g.Aircraft_timestamp BETWEEN @From AND @To
                        ORDER BY g.Aircraft_timestamp DESC";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Tail", tailNumber);
                        cmd.Parameters.AddWithValue("@From", from);
                        cmd.Parameters.AddWithValue("@To", to);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                records.Add(new TelemetryRecord
                                {
                                    TailNumber = reader.GetString(0),
                                    SequenceNumber = reader.GetInt32(1),
                                    Timestamp = reader.GetDateTime(2),
                                    AccelX = (double)reader.GetDecimal(3),
                                    AccelY = (double)reader.GetDecimal(4),
                                    AccelZ = (double)reader.GetDecimal(5),
                                    Weight = (double)reader.GetDecimal(6),
                                    Altitude = (double)reader.GetDecimal(7),
                                    Pitch = (double)reader.GetDecimal(8),
                                    Bank = (double)reader.GetDecimal(9)
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }

            return records;
        }

        public IList<InvalidPacket> SearchInvalidPackets(string tailNumber, DateTime from, DateTime to)
        {
            var packets = new List<InvalidPacket>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"SELECT Stored_timestamp, Tail_number, Packet_sequence, Raw_packet
                        FROM ErrorLog
                        WHERE Tail_number = @Tail AND Stored_timestamp BETWEEN @From AND @To
                        ORDER BY Stored_timestamp DESC";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Tail", tailNumber);
                        cmd.Parameters.AddWithValue("@From", from);
                        cmd.Parameters.AddWithValue("@To", to);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                packets.Add(new InvalidPacket
                                {
                                    ReceivedAt = reader.GetDateTime(0),
                                    TailNumber = reader.GetString(1),
                                    SequenceNumber = reader.GetInt32(2),
                                    RawPacket = reader.GetString(3)
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }

            return packets;
        }

        public TelemetryRecord GetLatestTelemetry(string tailNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = @"SELECT TOP 1 g.Tail_number, g.Packet_sequence, g.Aircraft_timestamp,
                            g.Accel_x, g.Accel_y, g.Accel_z, g.Weight,
                            a.Altitude, a.Pitch, a.Bank
                        FROM GForceParameters g
                        INNER JOIN AttitudeParameters a 
                            ON g.Tail_number = a.Tail_number AND g.Packet_sequence = a.Packet_sequence
                        WHERE g.Tail_number = @Tail
                        ORDER BY g.Aircraft_timestamp DESC";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Tail", tailNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new TelemetryRecord
                                {
                                    TailNumber = reader.GetString(0),
                                    SequenceNumber = reader.GetInt32(1),
                                    Timestamp = reader.GetDateTime(2),
                                    AccelX = (double)reader.GetDecimal(3),
                                    AccelY = (double)reader.GetDecimal(4),
                                    AccelZ = (double)reader.GetDecimal(5),
                                    Weight = (double)reader.GetDecimal(6),
                                    Altitude = (double)reader.GetDecimal(7),
                                    Pitch = (double)reader.GetDecimal(8),
                                    Bank = (double)reader.GetDecimal(9)
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return null on error
            }

            return null;
        }
    }
}