using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Code_Vanta_Systems.Models.Enums;
using System.Collections.Generic;

namespace Code_Vanta_Systems.Models
{
    // Represents a single physical asset/unit in the field or in stock.
    // Replaces the unique asset properties of the old Fridge.cs.
    public class FridgeUnit
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key to the model/type definition
        [Required]
        public int FridgeModelId { get; set; }
        public virtual FridgeModel FridgeModel { get; set; }

        [Required, StringLength(100)]
        public string SerialNumber { get; set; } // Retained from Fridge.cs

        [Required]
        public FridgeStatus Status { get; set; } // Retained from Fridge.cs

        [Required]
        public FridgeCondition Condition { get; set; } // Retained from Fridge.cs

        public DateTime PurchaseDate { get; set; } // Retained from Fridge.cs

        // Foreign Key: Where the physical unit is currently located (Warehouse or Customer Site)
        [Required]
        public int LocationId { get; set; } // Retained from Fridge.cs
        public virtual Location Location { get; set; }

        // Optional: The technician currently responsible for this unit (for maintenance tracking)
        public string? TechnicianUserId { get; set; } // Retained from Fridge.cs
        public virtual ApplicationUser? Technician { get; set; }

        public bool IsDeleted { get; set; } = false; // Retained from Fridge.cs
        public DateTime? DeletedAt { get; set; } // Retained from Fridge.cs

        // Navigation Collections (Now linking to FridgeUnitId instead of FridgeId)
        public virtual ICollection<FridgeFault> Faults { get; set; }
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }
        public virtual ICollection<FridgeAllocation> Allocations { get; set; } // Only one active allocation
        public virtual ICollection<FridgeUsageLog> UsageLogs { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public virtual ICollection<SensorReading> SensorReadings { get; set; }
        public virtual ICollection<ServiceRecord> ServiceRecords { get; set; }

        public FridgeUnit()
        {
            Faults = new HashSet<FridgeFault>();
            Maintenances = new HashSet<FridgeMaintenance>();
            Allocations = new HashSet<FridgeAllocation>();
            UsageLogs = new HashSet<FridgeUsageLog>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            SensorReadings = new HashSet<SensorReading>();
            ServiceRecords = new HashSet<ServiceRecord>();
        }
    }
}