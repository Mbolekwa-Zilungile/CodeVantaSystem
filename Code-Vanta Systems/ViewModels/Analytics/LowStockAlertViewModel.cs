using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class LowStockAlertViewModel
    {
        public int FridgeModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int AvailableUnits { get; set; }
        public int TotalUnits { get; set; }
        public StockLevel StockLevel { get; set; }
    }
}
