using Code_Vanta_Systems.ViewModels.Analytics;

public class AnalyticsDashboardViewModel
{
    // Fridge Unit Analytics - For InventoryAnalyticsController
    public List<StatusCount> FridgeUnitStatus { get; set; } = new();
    public List<StatusCount> FridgeUnitsByLocation { get; set; } = new();
    public List<CategoryStockViewModel> FridgeModelsByCategory { get; set; } = new();
    public List<StatusCount> RentalRequestStatus { get; set; } = new();

    // Fridge Analytics - For CustomerLiaisonAnalyticsController
    public List<StatusCount> FridgeStatus { get; set; } = new();
    public List<StatusCount> FridgeRegion { get; set; } = new();
    public List<CategoryStockViewModel> FridgeCategories { get; set; } = new();
    public List<StatusCount> OrderStatus { get; set; } = new();

    // Fridge Model Analytics
    public List<FridgeModelAnalyticsViewModel> AllFridgeModels { get; set; } = new();

    // Allocation & Rental Analytics
    public List<AllocationTrend> Allocations { get; set; } = new();
    public List<StatusCount> CustomerStatus { get; set; } = new();

    // Supplier & Maintenance Analytics
    public List<SupplierPerformanceViewModel> SupplierPerformance { get; set; } = new();
    public List<StatusCount> MaintenanceStats { get; set; } = new();
    public List<StatusCount> FaultStats { get; set; } = new();

    // Date Range
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Summary Statistics
    public int TotalFridgeModels { get; set; }
    public int TotalFridgeUnits { get; set; }
    public int AvailableFridgeUnits { get; set; }
    public int UnitsInRepair { get; set; }
    public decimal TotalInventoryValue { get; set; }
}