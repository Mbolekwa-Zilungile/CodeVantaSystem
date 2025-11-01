using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.ViewModels.PurchaseRequest
{
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.ComponentModel.DataAnnotations;

    namespace Code_Vanta_Systems.ViewModels
    {
        public class PurchaseRequestItemViewModel
        {
            public int? PurchaseRequestItemId { get; set; }

            [Required(ErrorMessage = "Fridge Model selection is required")]
            public int FridgeModelId { get; set; }  // ← Changed from FridgeId to FridgeModelId

            [Required(ErrorMessage = "Quantity is required")]
            [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
            public int Quantity { get; set; }

            public string FridgeModelName { get; set; }

            // For dropdown
            public List<SelectListItem> AvailableModels { get; set; } = new List<SelectListItem>();
        }
    }
}
