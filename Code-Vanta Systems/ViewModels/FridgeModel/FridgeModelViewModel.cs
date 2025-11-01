using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.FridgeModel
{
    public class FridgeModelViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public FridgeCategory Category { get; set; }
        public decimal RentalPricePerMonth { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int SupplierId { get; set; }
        public int AvailableUnits { get; set; }
        public int TotalUnits { get; set; }
        public int UnitsInRepair { get; set; }
    }
}
