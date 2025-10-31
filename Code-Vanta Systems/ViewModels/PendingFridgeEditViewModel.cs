using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class PendingFridgeEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SerialNumber { get; set; }

        [Required]
        public FridgeCategory Category { get; set; }

        [Required]
        public FridgeCondition Condition { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Rental price must be positive")]
        public decimal RentalPricePerMonth { get; set; }
    }
}

