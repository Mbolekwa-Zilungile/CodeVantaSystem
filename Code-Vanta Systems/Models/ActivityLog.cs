using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{      
    public class ActivityLog
    {
        public int Id { get; set; }

        // Name of the entity affected by the action (e.g "Fridge", "Fault")
        [Required]
        public string EntityName { get; set; }

        // Type of action performed (e.g., "Create", "Update", "Delete")
        [Required]
        public string Action { get; set; }

        // Foreign key referencing the user who performed the action
            [Required]
        public string PerformedByUserId { get; set; }
            
        // Navigation property for the user who performed the action
        public ApplicationUser PerformedByUser { get; set; }

        // Timestamp of when the action occurred (defaults to UTC now)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Optional details about the action (e.g., what changed
        public string Details { get; set; }
        

    }
}
