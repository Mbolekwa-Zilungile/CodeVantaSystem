using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateFridgeViewModel
    {
        
        public int Id { get; set; }

        [Required]
        public string Model { get; set; }

        public string SerialNumber { get; set; }

        [Required]
        public FridgeCategory Category { get; set; }

        [Required]
        public FridgeStatus Status { get; set; }

        [Required]
        public int SupplierId { get; set; }

        public int? LocationId { get; set; }

        [Required]
        public decimal RentalPricePerMonth { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public IFormFile ImageFile { get; set; }

        // Dropdown lists
        public List<SupplierDropdownViewModel> Suppliers { get; set; }
        public List<LocationDropdownViewModel> Locations { get; set; }

        // Nested classes for dropdowns
        public class SupplierDropdownViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class LocationDropdownViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}

