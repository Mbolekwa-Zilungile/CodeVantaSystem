using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Azure.Core.HttpHeader;

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
