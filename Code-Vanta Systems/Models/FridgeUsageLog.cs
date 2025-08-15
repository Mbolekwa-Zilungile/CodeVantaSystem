using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeUsageLog
    {

        public int Id { get; set; }

        // Link to the fridge being accessed
        [Required]
        public int FridgeId { get; set; }
        public Fridge Fridge { get; set; }

        // Timestamp of access
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

        // Who accessed the fridge (technician, employee, etc.)
        [Required]
        public string AccessedByUserId { get; set; }
        public ApplicationUser AccessedByUser { get; set; }

        // notes about the access
        public string Notes { get; set; }

    }
}
