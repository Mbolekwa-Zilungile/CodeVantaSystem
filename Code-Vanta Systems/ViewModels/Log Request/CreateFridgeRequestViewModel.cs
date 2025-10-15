using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Log_Request
{
    public class CreateFridgeRequestViewModel
    {
        [Required]
        [Display(Name = "Fridge Model")]
        public int FridgeId { get; set; }
        [Required]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string Notes { get; set; }
    }
}
