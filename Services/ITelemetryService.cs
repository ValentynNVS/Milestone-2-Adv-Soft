/*
File         : ITelemetryService.cs
Project      : SENG3020 - Term Project
Programmer   : Valentyn Novosydliuk
File Version : 11/28/2025
Description  : Defines the service interface for retrieving telemetry data invalid
               packets and database status for the Ground Terminal application.
*/

using System;
using System.Collections.Generic;
using FDMS.GroundTerminal.Models;

namespace FDMS.GroundTerminal.Services
{
    /*
     Interface     : ITelemetryService
     Description   : Provides contract definitions for telemetry retrieval search
                     operations and database status checks.
    */
    public interface ITelemetryService
    {
        /*
         Function     : GetTailNumbers
         Description  : Retrieves all available aircraft tail identifiers.
         Parameters   : none
         Return Values: IList<string>
        */
        IList<string> GetTailNumbers();

        /*
         Function     : GetLatestTelemetry
         Description  : Retrieves the latest telemetry record for a specified tail.
         Parameters   : tailNumber - aircraft identifier
         Return Values: TelemetryRecord
        */
        TelemetryRecord GetLatestTelemetry(string tailNumber);

        /*
         Function     : SearchTelemetry
         Description  : Searches for telemetry records for a specific tail within a
                        given time range.
         Parameters   : tailNumber - aircraft identifier
                        from       - starting timestamp
                        to         - ending timestamp
         Return Values: IList<TelemetryRecord>
        */
        IList<TelemetryRecord> SearchTelemetry(
            string tailNumber,
            DateTime from,
            DateTime to);

        /*
         Function     : SearchInvalidPackets
         Description  : Retrieves invalid packet logs based on tail and time range.
         Parameters   : tailNumber - aircraft identifier
                        from       - starting timestamp
                        to         - ending timestamp
         Return Values: IList<InvalidPacket>
        */
        IList<InvalidPacket> SearchInvalidPackets(
            string tailNumber,
            DateTime from,
            DateTime to);

        /*
         Function     : GetDatabaseStatus
         Description  : Returns database connectivity status information.
         Parameters   : none
         Return Values: DatabaseStatus
        */
        DatabaseStatus GetDatabaseStatus();
    }
}
