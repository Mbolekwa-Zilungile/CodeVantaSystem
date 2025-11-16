using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class ServiceRecord
    {
        [Key]
        public int Id { get; set; }

        // FIX: Uncommented FridgeId as controller logic is attempting to assign it
        [Required]
        public int FridgeId { get; set; }

        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        // ✅ FIX 1: Add FridgeUnitId foreign key (Must be nullable if old records exist, or NOT NULL if data is clean)
        [Required]
        public int FridgeUnitId { get; set; }

        [ForeignKey(nameof(FridgeUnitId))]
        public FridgeUnit FridgeUnit { get; set; }

        [Required]
        public DateTime DateOfService { get; set; }

        // User who performed the service
        [Required]
        public string TechnicianUserId { get; set; } // Renamed in controller to TechnicianId, but keeping model name for consistency

        [ForeignKey("TechnicianUserId")]
        public ApplicationUser Technician { get; set; }

        [Required]
        [StringLength(500)]
        public string ServiceDetails { get; set; } // Renamed in controller to Description, but keeping model name for consistency

        // Optional: Reference to a fault that was resolved
        public int? FaultId { get; set; }
        public FridgeFault Fault { get; set; }

        // NEW PROPERTY: Add this to track parts used
        [StringLength(500)]
        public string? PartsUsed { get; set; }

        // FIX: Added missing properties used by the Controller
        public int? MaintenanceTypeId { get; set; } // Tracks the type of service performed

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Cost { get; set; }
    }
}