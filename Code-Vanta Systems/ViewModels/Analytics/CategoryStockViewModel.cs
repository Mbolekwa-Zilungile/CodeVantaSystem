public class CategoryStockViewModel
{
    public string Category { get; set; } = string.Empty;
    public int TotalStock { get; set; }
    public int HighStock { get; set; }
    public int MediumStock { get; set; }
    public int LowStock { get; set; }
    public int CriticalStock { get; set; }

    // Additional properties for InventoryAnalyticsController
    public int TotalModels { get; set; }
    public int AvailableUnits { get; set; }
    public int TotalUnits { get; set; }
    public int UnitsInRepair { get; set; }
}