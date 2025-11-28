/*
File         : DatabaseStatus.cs
Project      : SENG3020 - Term Project
Programmer   : Valentyn Novosydliuk
File Version : 11/28/2025
Description  : Defines a model used to represent the database connection status
               including connectivity flag server name database name and a status message.
*/

namespace FDMS.GroundTerminal.Models
{
    /*
     Class        : DatabaseStatus
     Description  : Holds the results of a database connection test for display in
                    the Ground Terminal Settings tab.
    */
    public class DatabaseStatus
    {
        public bool IsConnected { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string StatusMessage { get; set; }
    }
}
