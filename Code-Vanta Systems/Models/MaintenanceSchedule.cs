using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class MaintenanceSchedule
    {
        [Key]
        public int Id { get; set; }

        // OLD: FridgeId -> NEW: FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }

        public FridgeUnit FridgeUnit { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public string TechnicianId { get; set; }

        public ApplicationUser Technician { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }
    }
}