using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

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

        public virtual ICollection<FridgeFault> Faults { get; set; }
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }

        // Renamed to match OnModelCreating
        public virtual ICollection<FridgeServiceHistory> ServiceHistories { get; set; }

        // Added for completeness
        public virtual ICollection<FridgeUsageLog> UsageLogs { get; set; }
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }

        public Fridge()
        {
            Faults = new HashSet<FridgeFault>();
            Maintenances = new HashSet<FridgeMaintenance>();
            Allocations = new HashSet<FridgeAllocation>();
            ServiceHistories = new HashSet<FridgeServiceHistory>();
            UsageLogs = new HashSet<FridgeUsageLog>();
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
        }
    }
}
