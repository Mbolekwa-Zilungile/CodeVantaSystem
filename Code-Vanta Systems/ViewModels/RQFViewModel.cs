using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RQFViewModel
    {
        public int PurchaseRequestId { get; set; }

        [Display(Name = "Requested Date")]
        [DataType(DataType.Date)]
        public DateTime RequestedDate { get; set; } = DateTime.Now;

       
        [Display(Name = "Expected Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        [Display(Name = "Priority")]
        public PriorityLevel? Priority { get; set; }

        [Display(Name = "Contact Person")]
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        public string? PurchaseRequestNotes { get; set; }

        public RequestStatus Status { get; set; }

        // Suppliers list
        public List<Supplier> Suppliers { get; set; } = new List<Supplier>();

        [Required(ErrorMessage = "Please select at least one supplier.")]
        [MinLength(1, ErrorMessage = "Please select at least one supplier.")]
        public List<int> SelectedSupplierIds { get; set; } = new List<int>();

        [MaxLength(500)]
        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        // Purchase request items
        public List<PurchaseRequestItemViewModel> PurchaseRequestItems { get; set; } = new List<PurchaseRequestItemViewModel>();
        public Company? Company { get; set; }
    }
}
