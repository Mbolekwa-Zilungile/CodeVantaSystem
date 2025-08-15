using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class TechnicianSchedule
    {

        [Key]
        public int ScheduleId { get; set; }

        // Foreign key referencing the technician assigned to the task
        [Required]
        public string TechnicianUserId { get; set; }

        // Navigation property for the technician
        [ForeignKey("TechnicianUserId")]
        public ApplicationUser Technician { get; set; }

        // Date and time the task is scheduled for
        [Required]
        public DateTime ScheduledDate { get; set; }

        // Foreign key referencing the location of the task
        [Required]
        public int LocationId { get; set; }

        // Navigation property for the location
        [ForeignKey("LocationId")]
        public Location Location { get; set; }

        // Type of task (e.g Maintenance, FaultRepair)
        [Required]
        [MaxLength(50)]
        public string TaskType { get; set; }

        // Reference ID to the specific task (e.g FridgeMaintenance or FridgeFault)
        public int TaskReferenceId { get; set; }

        // Status of the scheduled task (e.g Scheduled, Completed, Cancelled)
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } 
    }
}
