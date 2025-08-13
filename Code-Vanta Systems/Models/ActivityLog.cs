using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{      
    public class ActivityLog
    {
            public int Id { get; set; }

            [Required]
            public string EntityName { get; set; } // e.g., "Fridge", "Fault"

            [Required]
            public string Action { get; set; } // e.g., "Create", "Update", "Delete"

            [Required]
            public string PerformedByUserId { get; set; }
            public ApplicationUser PerformedByUser { get; set; }

            public DateTime Timestamp { get; set; } = DateTime.UtcNow;

            public string Details { get; set; }
        

    }
}
