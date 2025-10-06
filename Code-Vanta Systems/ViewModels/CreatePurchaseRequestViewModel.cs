using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    using Microsoft.AspNetCore.Mvc.Rendering;

    public class CreatePurchaseRequestViewModel
    {

        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public PriorityLevel Priority { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "At least one item is required")]
        public List<PurchaseRequestItemViewModel> Items { get; set; } = new();

        // ✅ Add this to populate the dropdown in the view
        public List<SelectListItem> AvailableItems { get; set; } = new();
        public RequestStatus Status { get; set; }
    }

}
