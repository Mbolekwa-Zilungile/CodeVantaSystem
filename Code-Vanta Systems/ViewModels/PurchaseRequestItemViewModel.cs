using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.ViewModels
{
    public class PurchaseRequestItemViewModel
    {
        [Required(ErrorMessage = "You must select a fridge")]
        public int FridgeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        // For dropdown in the view
        public List<SelectListItem> AvailableItems { get; set; } = new();

        public int? PurchaseRequestItemId { get; set; }

        // ✅ Add this so you can display fridge model in RFQ views
        [BindNever]
        public string FridgeModel { get; set; }
    }
}
