namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class AllocationAnalyticsViewModel
    {
        public DateTime AllocationDate { get; set; }
        public string FridgeModelName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime? DeallocationDate { get; set; }
        public double AllocationDuration { get; set; }
    }
}
