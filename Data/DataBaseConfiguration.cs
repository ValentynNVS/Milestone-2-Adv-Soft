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
