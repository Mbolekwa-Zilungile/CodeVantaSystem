using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class TechnicianSchedule
    {

        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public string TechnicianUserId { get; set; }

        [ForeignKey("TechnicianUserId")]
        public ApplicationUser Technician { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public Location Location { get; set; }

        [Required]
        [MaxLength(50)]
        public string TaskType { get; set; } // Maintenance, FaultRepair

        public int TaskReferenceId { get; set; } // FK to FridgeMaintenance or FridgeFault

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // Scheduled, Completed, Cancelled

    }
}
