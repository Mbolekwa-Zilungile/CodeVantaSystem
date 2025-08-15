using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class WorkFlowApproval
    {

        [Key]
        public int ApprovalId { get; set; }

        // Type of request being approved (e.g "PurchaseRequest")
        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; }

        // ID of the request being approved
        [Required]
        public int RequestId { get; set; }

        // Foreign key referencing the user who approved or rejected the request
        [Required]
        public string ApproverUserId { get; set; }

        // Navigation property for the approver
        [ForeignKey("ApproverUserId")]
        public ApplicationUser ApproverUser { get; set; }

        // Status of the approval (e.g., Pending, Approved, Rejected)
        [Required]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; }

        // Date when the approval decision was made (nullable for pending)
        public DateTime? ApprovalDate { get; set; }

        // Optional comments or notes from the approver
        [MaxLength(500)]
        public string Comments { get; set; }

    }
}
