using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RQFViewModel
    {
        public int PurchaseRequestId { get; set; }

        public string? PurchaseRequestNotes { get; set; }

        public DateTime RequestDate { get; set; }

        public DateTime? ExpectedDate { get; set; }

        public RequestStatus Status { get; set; }

        public PriorityLevel Priority { get; set; }

        // Initialize list to avoid null reference
        public List<Supplier> Suppliers { get; set; } = new List<Supplier>();

        [Required(ErrorMessage = "Please select at least one supplier.")]
        [MinLength(1, ErrorMessage = "Please select at least one supplier.")]
        public List<int> SelectedSupplierIds { get; set; } = new List<int>();

        [MaxLength(500)]
        public string? Notes { get; set; }

        public string? ContactPersonName { get; set; }

        // Initialize list to avoid null reference
        public List<PurchaseRequestItemViewModel> PurchaseRequestItems { get; set; } = new List<PurchaseRequestItemViewModel>();
    }
}
