namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class CustomerAnalyticsViewModel
    {
        public int CustomerId { get; set; }
        public string BusinessName { get; set; }
        public string ContactName { get; set; }
        public string BusinessType { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public int TotalAllocations { get; set; }
        public int ActiveAllocations { get; set; }
        public DateTime? FirstAllocationDate { get; set; }
        public DateTime? LastAllocationDate { get; set; }
        public string PreferredFridgeModel { get; set; }
    }
}
