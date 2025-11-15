using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class MaintenanceSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }

        // This property now relies entirely on MaintenanceScheduleConfiguration.cs
        public Fridge Fridge { get; set; }

        // FIX: Added FridgeUnitId and navigation property
        [Required]
        public int FridgeUnitId { get; set; }

        [ForeignKey(nameof(FridgeUnitId))]
        public virtual FridgeUnit FridgeUnit { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public string TechnicianId { get; set; }

        // This property now relies on conventions or other configurations
        public ApplicationUser Technician { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

    }
}