using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class WorkFlowApproval
    {

        [Key]
        public int ApprovalId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; } // e.g., "PurchaseRequest"

        [Required]
        public int RequestId { get; set; }

        [Required]
        public string ApproverUserId { get; set; }

        [ForeignKey("ApproverUserId")]
        public ApplicationUser ApproverUser { get; set; }

        [Required]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } // Pending, Approved, Rejected

        public DateTime? ApprovalDate { get; set; }

        [MaxLength(500)]
        public string Comments { get; set; }

    }
}
