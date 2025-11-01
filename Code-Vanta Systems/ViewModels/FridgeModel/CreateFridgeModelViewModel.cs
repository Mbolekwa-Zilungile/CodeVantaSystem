using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.ViewModels.Shared;

namespace Code_Vanta_Systems.ViewModels.FridgeModel
{
    public class CreateFridgeModelViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Model name is required")]
        [StringLength(100, ErrorMessage = "Model name cannot exceed 100 characters")]
        [Display(Name = "Model Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public FridgeCategory Category { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Rental Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental Price must be positive")]
        [Display(Name = "Rental Price Per Month")]
        public decimal RentalPricePerMonth { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile ImageFile { get; set; }

        public string ImageUrl { get; set; }

        public List<SupplierDropdownViewModel> Suppliers { get; set; } = new();
    }
}
