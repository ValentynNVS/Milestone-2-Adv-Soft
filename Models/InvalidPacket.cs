/*
File         : InvalidPacket.cs
Project      : SENG3020 - Term Project
Programmer   : Valentyn Novosydliuk
File Version : 11/22/2025
Description  : Defines a model representing an invalid telemetry packet including
               timestamp tail number sequence number reason and raw message data.
*/

using System;

namespace FDMS.GroundTerminal.Models
{
    /*
     Class        : InvalidPacket
     Description  : Represents malformed or rejected telemetry packets stored for
                    analysis and troubleshooting.
    */
    public class InvalidPacket
    {
        public DateTime ReceivedAt { get; set; }
        public string TailNumber { get; set; }
        public int SequenceNumber { get; set; }
        public string Reason { get; set; }
        public string RawPacket { get; set; }
    }
}
