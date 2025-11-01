using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class FridgeModelAnalyticsViewModel
    {
        // Basic Properties
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal RentalPricePerMonth { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        // Unit Count Properties
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int AllocatedUnits { get; set; }
        public int UnitsInRepair { get; set; }
        public int UnitsAllocated { get; set; }

        // Analytics Properties
        public double UtilizationRate { get; set; }
        public int AverageUnitAge { get; set; }
        public decimal Value { get; set; }
    }
}
