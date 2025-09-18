using System;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class FridgeMaintenance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        [Required]
        public string TechnicianId { get; set; }
        public virtual ApplicationUser Technician { get; set; }

        [Required]
        public DateTime MaintenanceDate { get; set; }

        [Required, StringLength(500)]
        public string MaintenanceDescription { get; set; }

        [Required]
        public MaintenanceStatus Status { get; set; }

        // Optional link to service history record
        public virtual FridgeServiceHistory ServiceHistoryRecord { get; set; }
    }
}
