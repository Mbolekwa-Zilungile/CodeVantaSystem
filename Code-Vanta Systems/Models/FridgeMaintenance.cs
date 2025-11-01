using System;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class FridgeMaintenance
    {
        [Key]
        public int Id { get; set; }

        // CHANGE: FridgeId → FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }
        public virtual FridgeUnit FridgeUnit { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        [Required]
        public string TechnicianId { get; set; }
        public virtual ApplicationUser Technician { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime MaintenanceDate { get; set; }

        public DateTime? ActualCompletionDate { get; set; }

        [Required, StringLength(500)]
        public string MaintenanceDescription { get; set; }

        // ADD THESE MISSING PROPERTIES:
        [Required]
        public MaintenanceType MaintenanceType { get; set; } // Add this enum

        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; }

        [Range(0, int.MaxValue)]
        public int DurationHours { get; set; }

        [Required]
        public MaintenanceStatus Status { get; set; }
    }
}