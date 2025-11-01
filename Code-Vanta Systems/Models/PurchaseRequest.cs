using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string RequestNumber { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public string RequestedByUserId { get; set; }
        public virtual ApplicationUser RequestedBy { get; set; }

        [Required]
        public RequestStatus Status { get; set; }

        public PriorityLevel Priority { get; set; } // Not RequestPriority?

        [DataType(DataType.Date)]
        [Display(Name = "Expected Delivery Date")]
        public DateTime? ExpectedDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual ICollection<PurchaseRequestItem> RequestedItems { get; set; } = new HashSet<PurchaseRequestItem>();
        public virtual ICollection<Quotation> Quotations { get; set; } = new HashSet<Quotation>();
    }
}