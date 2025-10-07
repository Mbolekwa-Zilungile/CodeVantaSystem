using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Code_Vanta_Systems.ViewModels
{
    public class PurchaseRequestItemViewModel
    {
   
            [Required(ErrorMessage = "You must select a fridge")]
            public int FridgeId { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
            public int Quantity { get; set; }

            // For the dropdown in the view
            public List<SelectListItem> AvailableItems { get; set; } = new();
        public int? PurchaseRequestItemId { get; set; }
    }
}
