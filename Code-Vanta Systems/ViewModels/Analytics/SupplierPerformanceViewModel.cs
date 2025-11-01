namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class SupplierPerformanceViewModel
    {
        public string SupplierName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal OnTimeDeliveryRate { get; set; }
        public decimal QualityRating { get; set; }

        // Additional properties for InventoryAnalyticsController
        public int TotalModels { get; set; }
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
    }
}
