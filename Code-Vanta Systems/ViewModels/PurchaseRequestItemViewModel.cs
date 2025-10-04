using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class PurchaseRequestItemViewModel
    {
        [Required]
        [Display(Name = "Item")]
        public int ItemCatalogId { get; set; }   // Selected item from dropdown

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        // Optional: Add a SelectList for the dropdown
        [ValidateNever]
        public IEnumerable<SelectListItem> AvailableItems { get; set; }
    }
}