using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateFridgeViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Fridge Model")]
        public string Model { get; set; }

        [Required]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Required]
        [Display(Name = "Status")]
        public FridgeStatus Status { get; set; }

        [Required]
        [Display(Name = "Category")]
        public FridgeCategory Category { get; set; }

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        [DataType(DataType.Upload)]
        [Display(Name = "Fridge Image")]
        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Rental Price per Month (ZAR)")]
        public decimal RentalPricePerMonth { get; set; }

        [Required]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Dropdowns
        public List<SupplierDropdownViewModel> Suppliers { get; set; } = new();
        public List<LocationDropdownViewModel> Locations { get; set; } = new();

        // Nested classes for dropdowns
        public class SupplierDropdownViewModel { public int Id { get; set; } public string Name { get; set; } }
        public class LocationDropdownViewModel { public int Id { get; set; } public string Name { get; set; } }
    }
}
