using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Customer
{
    public class FridgeCatalogViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public FridgeCategory Category { get; set; }
        public decimal PricePerMonth { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string SupplierName { get; set; }
        public int AvailableUnits { get; set; }
    }
}
