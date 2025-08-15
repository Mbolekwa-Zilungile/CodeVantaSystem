using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class AuditTrail
    {

        [Key]
        public int AuditId { get; set; }

        // Name of the entity that was changed (e.g., "Customer", "Fridge")
        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; }

        // ID of the specific entity instance that was changed
        [Required]
        public int EntityId { get; set; }

        // Foreign key referencing the user who made the change
        [Required]
        public string ChangedByUserId { get; set; }

        // Navigation property for the user who made the change
        [ForeignKey("ChangedByUserId")]
        public ApplicationUser ChangedByUser { get; set; }

        // Type of change performed (e.g "Create", "Update", "Delete")
        [Required]
        [MaxLength(20)]
        public string ChangeType { get; set; }

        // Date and time when the change occurred
        [Required]
        public DateTime ChangeDate { get; set; }

        // Details of the change, stored as JSON or plain text
        public string ChangeDetails { get; set; }
    }
}
