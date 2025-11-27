using System;

namespace FDMS.GroundTerminal.Models
{
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
    }
}
