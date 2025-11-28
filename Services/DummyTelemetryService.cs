/*
File         : DummyTelemetryService.cs
Project      : SENG3020 - Term Project
Programmer   : Valentyn Novosydliuk
File Version : 11/28/2025
Description  : Provides a dummy implementation of ITelemetryService that generates
               simulated telemetry records invalid packets and database status data.
*/

using System;
using System.Collections.Generic;
using FDMS.GroundTerminal.Models;

namespace FDMS.GroundTerminal.Services
{
    /*
     Class        : DummyTelemetryService
     Description  : Generates mock telemetry values and invalid packet data for UI
                    testing without requiring a live database connection.
    */
    public class DummyTelemetryService : ITelemetryService
    {
        private readonly Random _random = new Random();
        private readonly List<string> _tailNumbers;

        /*
         Function     : DummyTelemetryService
         Description  : Initializes the dummy service with predefined tail numbers for
                        use in telemetry simulation.
         Parameters   : none
         Return Values: void
        */
        public DummyTelemetryService()
        {
            _tailNumbers = new List<string>
            {
                "C-ABCD",
                "C-EFGH",
                "C-IJKL"
            };
        }

        /*
         Function     : GetTailNumbers
         Description  : Returns the list of available dummy tail numbers.
         Parameters   : none
         Return Values: IList<string>
        */
        public IList<string> GetTailNumbers()
        {
            return _tailNumbers;
        }

        /*
         Function     : GetLatestTelemetry
         Description  : Generates a simulated telemetry record for live mode including
                        accelerometer altitude weight and attitude values.
         Parameters   : tailNumber - aircraft tail identifier
         Return Values: TelemetryRecord
        */
        public TelemetryRecord GetLatestTelemetry(string tailNumber)
        {
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

        /*
         Function     : SearchTelemetry
         Description  : Generates a list of simulated telemetry records for the given
                        time range in one minute increments.
         Parameters   : tailNumber - aircraft identifier
                        from       - starting timestamp
                        to         - ending timestamp
         Return Values: IList<TelemetryRecord>
        */
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

        /*
         Function     : SearchInvalidPackets
         Description  : Returns a small set of simulated invalid packet records using
                        random sequence numbers and consistent error reasons.
         Parameters   : tailNumber - aircraft identifier
                        from       - starting timestamp
                        to         - ending timestamp
         Return Values: IList<InvalidPacket>
        */
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

        /*
         Function     : GetDatabaseStatus
         Description  : Returns a dummy database status object since this service does
                        not actually connect to a real database.
         Parameters   : none
         Return Values: DatabaseStatus
        */
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

        /*
         Function     : NextDouble
         Description  : Generates a pseudo random double value within a fixed range.
         Parameters   : minValue - lower bound
                        maxValue - upper bound
         Return Values: double
        */
        private double NextDouble(double minValue, double maxValue)
        {
            return minValue + (_random.NextDouble() * (maxValue - minValue));
        }
    }
}
