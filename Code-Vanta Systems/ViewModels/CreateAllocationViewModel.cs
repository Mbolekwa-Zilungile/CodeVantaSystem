using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateAllocationViewModel
    {
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

 
        public IEnumerable<SelectListItem>? Fridges { get; set; }
        public IEnumerable<SelectListItem>? Customers { get; set; }
    }
}
