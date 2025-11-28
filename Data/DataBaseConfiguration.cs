/*
File         : DataBaseConfiguration.cs
Project      : SENG3020 - Term Project
Programmer   : Ygnacio Maza Sanchez
File Version : 11/28/2025
Description  : Provides helper to retrieve the FDMS connection string from App.config or build a default one.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;

namespace FDMS.GroundTerminal.Data
{
    public static class DataBaseConfiguration
    {
        /*
          Function: GetConnectionString()
          Description: Returns the configured connection string named "FDMSConnection" from App.config.
                       If not present, builds and returns a sane default connection string.
          Parameters: none
          Return Values: string 
        */
        public static string GetConnectionString()
        {
            var setting = ConfigurationManager.ConnectionStrings["FDMSConnection"];

            if (setting != null)
            {
                return setting.ConnectionString;
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = @"localhost\SQLEXPRESS",
                InitialCatalog = "FDMS",
                IntegratedSecurity = true,
                TrustServerCertificate = true
            };
            return builder.ConnectionString;
        }
    }
}
