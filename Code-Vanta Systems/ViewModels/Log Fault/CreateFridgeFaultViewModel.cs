using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Log_Fault
{
    public class CreateFridgeFaultViewModel
    {
        // Fridge Selection
        [Required]
        [Display(Name = "Fridge")]
        public int FridgeId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Fault Description")]
        public string FaultDescription { get; set; }

        [Required]
        public FaultStatus Status { get; set; }
    }
}
