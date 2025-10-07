using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateFridgeViewModel
    {
        [Required(ErrorMessage = "Category is required")]
        public FridgeCategory Category { get; set; }

        [Required(ErrorMessage = "Model is required")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Serial Number is required")]
        public string SerialNumber { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Image is required")]
        public IFormFile ImageFile { get; set; }

        [Required(ErrorMessage = "Purchase Date is required")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Rental Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental Price must be positive")]
        public decimal RentalPricePerMonth { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public FridgeStatus Status { get; set; } = FridgeStatus.Available;

        [Required]
        public string StockQuantity { get; set; }
    }
}
