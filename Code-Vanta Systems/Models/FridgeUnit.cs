using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class FridgeUnit
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string SerialNumber { get; set; } // UNIQUE for each physical fridge

        [Required]
        public FridgeStatus Status { get; set; } // InWarehouse, Allocated, InRepair, Scrapped

        [Required]
        public FridgeCondition Condition { get; set; } // New, Used, Refurbished

        public DateTime PurchaseDate { get; set; }

        [DataType(DataType.Currency)]
        public decimal PurchasePrice { get; set; } // Added missing property

        // Foreign Keys
        [Required]
        public int FridgeModelId { get; set; }
        public virtual FridgeModel FridgeModel { get; set; }

        // Current allocation (nullable if not allocated)
        public int? CurrentCustomerId { get; set; }
        public virtual Customer CurrentCustomer { get; set; }

        // Technician assignment
        public string? TechnicianUserId { get; set; }
        public virtual ApplicationUser? Technician { get; set; }

        // Location
        [Required]
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties for history
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }
        public virtual ICollection<FridgeFault> Faults { get; set; }
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }
        public virtual ICollection<FridgeUsageLog> UsageLogs { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public virtual ICollection<SensorReading> SensorReadings { get; set; }
        public virtual ICollection<ServiceRecord> ServiceRecords { get; set; }
        public virtual ICollection<RentedFridge> RentedFridges { get; set; }

        public FridgeUnit()
        {
            Allocations = new HashSet<FridgeAllocation>();
            Faults = new HashSet<FridgeFault>();
            Maintenances = new HashSet<FridgeMaintenance>();
            UsageLogs = new HashSet<FridgeUsageLog>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            SensorReadings = new HashSet<SensorReading>();
            ServiceRecords = new HashSet<ServiceRecord>();
            RentedFridges = new HashSet<RentedFridge>();
        }
    }
}