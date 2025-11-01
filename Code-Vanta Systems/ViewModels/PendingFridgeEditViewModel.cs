using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class PendingFridgeEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Required]
        [Display(Name = "Category")]
        public FridgeCategory Category { get; set; }

        [Required]
        [Display(Name = "Condition")]
        public FridgeCondition Condition { get; set; }

        [Required]
        [Display(Name = "Monthly Rental Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental price must be positive")]
        public decimal RentalPricePerMonth { get; set; }
    }
}

