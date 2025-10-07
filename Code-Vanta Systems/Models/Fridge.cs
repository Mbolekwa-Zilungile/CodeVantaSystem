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

        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        [Required]
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // Added for completeness
        public virtual ICollection<FridgeFault> Faults { get; set; }
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }
        public virtual ICollection<FridgeUsageLog> UsageLogs { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public virtual ICollection<SensorReading> SensorReadings { get; set; }

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
        public string StockQuantity { get; set; }

        public Fridge()
        {
            Faults = new HashSet<FridgeFault>();
            Maintenances = new HashSet<FridgeMaintenance>();
            Allocations = new HashSet<FridgeAllocation>();
            UsageLogs = new HashSet<FridgeUsageLog>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            SensorReadings = new HashSet<SensorReading>(); // Initialize new collection
        }
    }
}
