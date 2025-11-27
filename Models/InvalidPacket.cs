using System;

namespace FDMS.GroundTerminal.Models
{
    public class InvalidPacket
    {
        public DateTime ReceivedAt { get; set; }
        public string TailNumber { get; set; }
        public int SequenceNumber { get; set; }
        public string Reason { get; set; }
        public string RawPacket { get; set; }
    }
}
