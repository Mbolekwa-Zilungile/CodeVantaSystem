using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.PurchaseRequest.Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreatePurchaseRequestViewModel
    {
        public DateTime? ExpectedDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public PriorityLevel? Priority { get; set; }

        public string Notes { get; set; }

        [Required(ErrorMessage = "At least one fridge model is required")]
        [MinLength(1, ErrorMessage = "At least one fridge model is required")]
        public List<PurchaseRequestItemViewModel> Items { get; set; } = new List<PurchaseRequestItemViewModel>();
    }
}