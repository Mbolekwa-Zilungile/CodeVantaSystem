using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RentalRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeModelId { get; set; }
        public virtual FridgeModel FridgeModel { get; set; }

        [Required]
        public string RequestedByUserId { get; set; }
        public virtual ApplicationUser RequestedByUser { get; set; }

        // ADD THESE PROPERTIES:
        [Required]
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [Required]
        [Display(Name = "Requested Duration (months)")]
        public int RequestedDuration { get; set; } = 12;

        [StringLength(500)]
        [Display(Name = "Special Requirements")]
        public string SpecialRequirements { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled

        public bool IsPaid { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation to allocation (if approved and allocated)
        public int? AllocationId { get; set; }
        public virtual FridgeAllocation Allocation { get; set; }

        public DateTime? AllocationDate { get; set; }
    }
}