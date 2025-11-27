using System;
using System.Collections.Generic;
using FDMS.GroundTerminal.Models;

namespace FDMS.GroundTerminal.Services
{
    public interface ITelemetryService
    {
        IList<string> GetTailNumbers();

        TelemetryRecord GetLatestTelemetry(string tailNumber);

        IList<TelemetryRecord> SearchTelemetry(
            string tailNumber,
            DateTime from,
            DateTime to);

        IList<InvalidPacket> SearchInvalidPackets(
            string tailNumber,
            DateTime from,
            DateTime to);

        DatabaseStatus GetDatabaseStatus();
    }
}
