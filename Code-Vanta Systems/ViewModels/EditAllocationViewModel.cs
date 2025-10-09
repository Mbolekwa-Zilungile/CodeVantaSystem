using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class EditAllocationViewModel
    {
        [Required]
        public int Id { get; set; } // Allocation ID

        [Required]
        [Display(Name = "Fridge")]
        public int FridgeId { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        // Optional: lists for dropdowns
        public IEnumerable<SelectListItem>? Fridges { get; set; }
        public IEnumerable<SelectListItem>? Customers { get; set; }

        // For display or additional logic
        public int OriginalQuantity { get; set; } // To handle stock adjustments
    }
}
