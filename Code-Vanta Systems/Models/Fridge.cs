using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Models
{
    public class Fridge
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Model { get; set; }

        [Required, StringLength(100)]
        public string SerialNumber { get; set; }

        [Required]
        public FridgeStatus Status { get; set; }

        public DateTime PurchaseDate { get; set; }

        // 🎯 FIX 1: Make TechnicianUserId nullable (string?)
        // This is required by MaintenanceTechnicianController for filtering, but optional on creation.
        public string? TechnicianUserId { get; set; }
        // Assuming ApplicationUser is the entity for users
        public virtual ApplicationUser? Technician { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        [Required]
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // FIX 2: Make CustomerId nullable (int?)
        // Required for tracking where the fridge is allocated, but optional on creation.
        public int? CustomerId { get; set; }
        // Assuming Customer is the entity for customers. Navigation property must also be nullable.
        public virtual Customer? Customer { get; set; }

        // Added for completeness
        public virtual ICollection<FridgeFault> Faults { get; set; }
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }
        public virtual ICollection<FridgeUsageLog> UsageLogs { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public virtual ICollection<SensorReading> SensorReadings { get; set; }
        public virtual ICollection<ServiceRecord> ServiceRecords { get; set; }

        // Added New Fridge Props
        [Required]
        public FridgeCategory Category { get; set; }

        [StringLength(250)]
        public string ImageUrl { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        [Required(ErrorMessage = "Rental Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental Price must be positive")]
        [Precision(10,1)]
        public decimal RentalPricePerMonth { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        [Required]
        // track the permanent state of the fridge
        public FridgeCondition Condition { get; set; }

        public Fridge()
        {
            Faults = new HashSet<FridgeFault>();
            Maintenances = new HashSet<FridgeMaintenance>();
            Allocations = new HashSet<FridgeAllocation>();
            UsageLogs = new HashSet<FridgeUsageLog>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            SensorReadings = new HashSet<SensorReading>(); // Initialize new collection
            ServiceRecords = new HashSet<ServiceRecord>();
        }
    }
}
