using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDMS.GroundTerminal.Models;

namespace FDMS.GroundTerminal.Services
{
    public interface IDataBaseService
    {
        DatabaseStatus TestConnection();

        bool StoreTelemetry(TelemetryRecord record);

        bool StoreInvalidPacket(InvalidPacket record);

        IList<string> GetTailNumbers();

        IList<TelemetryRecord> SearchTelemetry(string tailNumber, DateTime from, DateTime to);
        IList<InvalidPacket> SearchInvalidPackets(string tailNumber, DateTime from, DateTime to);

        TelemetryRecord GetLatestTelemetry(string tailNumber);
    }
}
