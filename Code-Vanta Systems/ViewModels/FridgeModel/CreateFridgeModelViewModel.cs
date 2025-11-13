using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.InventoryLiaison
{
    public class CreateFridgeModelViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Model Name")]
        public string ModelName { get; set; }

        [Required]
        public FridgeCategory Category { get; set; }

        [Required(ErrorMessage = "Rental Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        [Display(Name = "Rental Price (Per Month)")]
        public decimal RentalPricePerMonth { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Display(Name = "Model Image")]
        [DataType(DataType.Upload)]
        // IFormFile is used for handling file uploads in MVC
        public IFormFile? ImageFile { get; set; }
    }
}