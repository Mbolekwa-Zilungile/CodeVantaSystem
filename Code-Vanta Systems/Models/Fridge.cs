using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class Fridge
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Model { get; set; }

        [Required, StringLength(100)]
        public string SerialNumber { get; set; }

        [Required]
        public FridgeStatus Status { get; set; }

        public DateTime PurchaseDate { get; set; }

        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // Navigation
        public virtual ICollection<FridgeFault> Faults { get; set; }
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }
        public virtual ICollection<FridgeServiceHistory> ServiceHistory { get; set; }
    }
}
