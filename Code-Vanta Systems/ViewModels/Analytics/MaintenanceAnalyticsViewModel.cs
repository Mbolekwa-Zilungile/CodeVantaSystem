namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class MaintenanceAnalyticsViewModel
    {
        public DateTime MaintenanceDate { get; set; }
        public string FridgeModelName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string MaintenanceType { get; set; } = string.Empty;
        public string TechnicianName { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public int DurationHours { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
