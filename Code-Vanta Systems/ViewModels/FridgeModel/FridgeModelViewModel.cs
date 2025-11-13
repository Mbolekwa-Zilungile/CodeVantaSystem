using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.InventoryLiaison
{
    public class FridgeModelViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Model Name")]
        public string ModelName { get; set; }

        [Display(Name = "Supplier")]
        public string SupplierName { get; set; }

        public FridgeCategory Category { get; set; }

        [Display(Name = "Rental Price")]
        [DataType(DataType.Currency)]
        public decimal RentalPricePerMonth { get; set; }

        [Display(Name = "Available Stock")]
        public int StockQuantity { get; set; } // Calculated from FridgeUnit where Status = InWarehouse
    }
}