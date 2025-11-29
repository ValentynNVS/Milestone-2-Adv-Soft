/*
File         : TelemetryRecord.cs
Project      : SENG3020 - Term Project
Programmer   : Valentyn Novosydliuk
File Version : 11/22/2025
Description  : Defines a telemetry record model containing sensor values flight
               attitude parameters and sequence information for a specific tail.
*/

using System;

namespace FDMS.GroundTerminal.Models
{
    /*
     Class        : TelemetryRecord
     Description  : Represents a full decoded telemetry packet including timestamp
                    accelerometer readings weight altitude pitch bank and metadata.
    */
    public class TelemetryRecord
    {
        public DateTime Timestamp { get; set; }
        public string TailNumber { get; set; }

        public double AccelX { get; set; }
        public double AccelY { get; set; }
        public double AccelZ { get; set; }
        public double Weight { get; set; }

        public double Altitude { get; set; }
        public double Pitch { get; set; }
        public double Bank { get; set; }

        public int SequenceNumber { get; set; }
        public bool IsFromRealTime { get; set; }
        public int Checksum { get; set; }
    }
}
