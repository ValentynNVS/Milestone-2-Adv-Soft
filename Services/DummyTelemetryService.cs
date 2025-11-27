using System;
using System.Collections.Generic;
using FDMS.GroundTerminal.Models;

namespace FDMS.GroundTerminal.Services
{
    public class DummyTelemetryService : ITelemetryService
    {
        private readonly Random _random = new Random();
        private readonly List<string> _tailNumbers;

        public DummyTelemetryService()
        {
            _tailNumbers = new List<string>
            {
                "C-ABCD",
                "C-EFGH",
                "C-IJKL"
            };
        }

        public IList<string> GetTailNumbers()
        {
            return _tailNumbers;
        }

        public TelemetryRecord GetLatestTelemetry(string tailNumber)
        {
            // Simulate some changing data
            return new TelemetryRecord
            {
                Timestamp = DateTime.Now,
                TailNumber = tailNumber,
                AccelX = NextDouble(-1.0, 1.0),
                AccelY = NextDouble(-1.0, 1.0),
                AccelZ = NextDouble(-1.0, 1.0),
                Weight = NextDouble(2000.0, 3000.0),
                Altitude = NextDouble(1000.0, 12000.0),
                Pitch = NextDouble(-10.0, 10.0),
                Bank = NextDouble(-15.0, 15.0),
                SequenceNumber = _random.Next(1, 10000),
                IsFromRealTime = true
            };
        }

        public IList<TelemetryRecord> SearchTelemetry(
            string tailNumber,
            DateTime from,
            DateTime to)
        {
            var list = new List<TelemetryRecord>();

            var time = from;
            while (time <= to)
            {
                list.Add(new TelemetryRecord
                {
                    Timestamp = time,
                    TailNumber = tailNumber,
                    AccelX = NextDouble(-1.0, 1.0),
                    AccelY = NextDouble(-1.0, 1.0),
                    AccelZ = NextDouble(-1.0, 1.0),
                    Weight = NextDouble(2000.0, 3000.0),
                    Altitude = NextDouble(1000.0, 12000.0),
                    Pitch = NextDouble(-10.0, 10.0),
                    Bank = NextDouble(-15.0, 15.0),
                    SequenceNumber = _random.Next(1, 10000),
                    IsFromRealTime = false
                });

                time = time.AddMinutes(1);
            }

            return list;
        }

        public IList<InvalidPacket> SearchInvalidPackets(
            string tailNumber,
            DateTime from,
            DateTime to)
        {
            var list = new List<InvalidPacket>();

            for (int i = 0; i < 5; i++)
            {
                list.Add(new InvalidPacket
                {
                    ReceivedAt = from.AddMinutes(i * 10),
                    TailNumber = tailNumber,
                    SequenceNumber = _random.Next(1, 10000),
                    Reason = "Checksum mismatch",
                    RawPacket = "RAW_DATA_" + i
                });
            }

            return list;
        }

        public DatabaseStatus GetDatabaseStatus()
        {
            return new DatabaseStatus
            {
                IsConnected = true,
                ServerName = "REMOTE-SQL-SERVER",
                DatabaseName = "FDMS",
                StatusMessage = "Dummy DB status (replace with real implementation)."
            };
        }

        private double NextDouble(double minValue, double maxValue)
        {
            return minValue + (_random.NextDouble() * (maxValue - minValue));
        }
    }
}
