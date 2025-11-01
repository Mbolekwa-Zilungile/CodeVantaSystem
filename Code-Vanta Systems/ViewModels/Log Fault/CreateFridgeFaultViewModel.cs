using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Log_Fault
{
    public class CreateFridgeFaultViewModel
    {
        [Required(ErrorMessage = "Fridge Unit is required")]
        [Display(Name = "Fridge Unit")]
        public int FridgeUnitId { get; set; } // ✅ Changed from FridgeId to FridgeUnitId

        [Required]
        [StringLength(500)]
        [Display(Name = "Fault Description")]
        public string FaultDescription { get; set; }

        [Required]
        public FaultStatus Status { get; set; }
    }
}
