using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.FridgeUnit;

namespace Code_Vanta_Systems.ViewModels.FridgeModel
{
    public class FridgeModelDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public FridgeCategory Category { get; set; }
        public decimal RentalPricePerMonth { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string SupplierName { get; set; }
        public List<FridgeUnitViewModel> FridgeUnits { get; set; } = new();
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int UnitsInRepair { get; set; }
        public int UnitsAllocated { get; set; }
    }
}
