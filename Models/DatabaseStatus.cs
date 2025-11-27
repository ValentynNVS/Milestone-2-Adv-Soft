namespace FDMS.GroundTerminal.Models
{
    public class DatabaseStatus
    {
        public bool IsConnected { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string StatusMessage { get; set; }
    }
}
