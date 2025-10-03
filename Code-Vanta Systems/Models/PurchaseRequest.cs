using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public string RequestedByUserId { get; set; }
        public virtual ApplicationUser RequestedBy { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [Required]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium; // Default value

        [DataType(DataType.Date)]
        [Display(Name = "Expected Delivery Date")]
        public DateTime? ExpectedDate { get; set; }  // Nullable, since it might not always be known upfront

        [StringLength(1000)]
        public string? Notes { get; set; }  // Optional notes field for extra info

        public virtual ICollection<PurchaseRequestItem> RequestedItems { get; set; } = new HashSet<PurchaseRequestItem>();

        public virtual ICollection<Quotation> Quotations { get; set; } = new HashSet<Quotation>();
    }
}