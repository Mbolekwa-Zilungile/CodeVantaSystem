using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FridgeRequests
{
    public class CreateRentalRequestViewModel
    {
        public int FridgeModelId { get; set; }
        public string FridgeModelName { get; set; }
        public FridgeCategory FridgeModelCategory { get; set; }
        public decimal RentalPricePerMonth { get; set; }

        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Requested duration is required")]
        [Range(1, 36, ErrorMessage = "Duration must be between 1 and 36 months")]
        [Display(Name = "Requested Duration (months)")]
        public int RequestedDuration { get; set; } = 12;

        [StringLength(500, ErrorMessage = "Special requirements cannot exceed 500 characters")]
        [Display(Name = "Special Requirements")]
        public string SpecialRequirements { get; set; }

        public int MaxQuantity { get; set; }
        public int AvailableUnits { get; set; }
    }
}
