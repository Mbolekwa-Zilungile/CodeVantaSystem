using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Log_Fault
{
    public class CustomerEditFridgeFaultViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Fridge")]
        public int FridgeId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Fault Description")]
        public string FaultDescription { get; set; }
    }
}
