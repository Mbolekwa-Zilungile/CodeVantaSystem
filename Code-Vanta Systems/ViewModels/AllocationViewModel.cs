using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class AllocationViewModel
    {
        [Required(ErrorMessage = "Fridge unit is required")]
        [Display(Name = "Fridge Unit")]
        public int FridgeUnitId { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        public string? TechnicianId { get; set; }
    }
}
