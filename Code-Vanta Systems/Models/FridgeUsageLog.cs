using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeUsageLog
    {
        public int Id { get; set; }

        // CHANGE: FridgeId → FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }
        public FridgeUnit FridgeUnit { get; set; }

        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string AccessedByUserId { get; set; }
        public ApplicationUser AccessedByUser { get; set; }

        public string Notes { get; set; }
    }
}
