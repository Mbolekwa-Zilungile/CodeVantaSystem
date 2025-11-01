namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class CustomerLiaisonAnalyticsViewModel
    {
        // Fridge Unit Analytics
        public List<StatusCount> FridgeUnitStatus { get; set; } = new();
        public List<StatusCount> FridgeUnitsByLocation { get; set; } = new();

        // Fridge Model Analytics
        public List<CategoryStockViewModel> FridgeModelsByCategory { get; set; } = new();

        // Customer & Rental Analytics
        public List<StatusCount> RentalRequestStatus { get; set; } = new();
        public List<StatusCount> CustomerStatus { get; set; } = new();
        public List<StatusCount> CustomerBusinessTypes { get; set; } = new();

        // Allocation Analytics
        public List<AllocationTrend> Allocations { get; set; } = new();

        // Date Range
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Summary Statistics - ADD THESE PROPERTIES
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int TotalFridgeModels { get; set; }
        public int TotalFridgeUnits { get; set; }
        public int AvailableFridgeUnits { get; set; }
        public int ActiveAllocations { get; set; }
        public int TotalRentalRequests { get; set; }
        public int PendingRentalRequests { get; set; }
    }
}
