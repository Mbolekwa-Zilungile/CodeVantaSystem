using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FridgeRequests
{
    public class RentalRequestViewModel
    {
        public int Id { get; set; }
        public int FridgeModelId { get; set; }
        public string FridgeModelName { get; set; }
        public FridgeCategory FridgeModelCategory { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Requested Duration (months)")]
        public int RequestedDuration { get; set; }

        [StringLength(500)]
        [Display(Name = "Special Requirements")]
        public string SpecialRequirements { get; set; }

        public string Status { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
