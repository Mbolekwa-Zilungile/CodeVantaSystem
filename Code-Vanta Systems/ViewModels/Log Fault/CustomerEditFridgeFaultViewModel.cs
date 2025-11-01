using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Log_Fault
{
    public class CustomerEditFridgeFaultViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Fridge Unit is required")]
        [Display(Name = "Fridge Unit")]
        public int FridgeUnitId { get; set; } // ✅ Changed from FridgeId to FridgeUnitId

        [Required]
        [StringLength(500)]
        [Display(Name = "Fault Description")]
        public string FaultDescription { get; set; }
    }
}
