using System;
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

        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        // FIX: Must be present. Used for when the maintenance is planned.
        [Required]
        public DateTime ScheduledDate { get; set; }

        // FIX: Must be present. Used for the service task notes.
        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        // FIX: Must be present. References the ApplicationUser who is assigned.
        [Required]
        public string TechnicianId { get; set; }

        [ForeignKey("TechnicianId")]
        public ApplicationUser Technician { get; set; }

        // FIX: Must be present. Status of the scheduled task (e.g Scheduled, Completed, Cancelled)
        [Required]
        [MaxLength(20)]
        public string Status { get; set; }
    }
}