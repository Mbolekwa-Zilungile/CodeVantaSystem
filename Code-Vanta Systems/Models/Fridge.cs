using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class Fridge
    {
        [Key]
        public int Id { get; set; }

        // Model name or identifier of the fridge
        [Required]
        [StringLength(100)]
        public string Model { get; set; }

        // Unique serial number of the fridge
        [Required] 
        [StringLength(100)]
        public string SerialNumber { get; set; }

        // Current status of the fridge (e.g., Active, Faulty, Scrapped)
        [Required]
        public FridgeStatus Status { get; set; }

        // Date when the fridge was purchased
        public DateTime PurchaseDate { get; set; }

        // Foreign key referencing the supplier of the fridge
        [Required]
        public int SupplierId { get; set; }

        // Navigation property for the supplier
        public virtual Supplier Supplier { get; set; }

        // Foreign key referencing the location of the fridge
        [Required]
        public int LocationId { get; set; }


        // Navigation property for the location
        public virtual Location Location { get; set; }

        // Navigation property for fault reports associated with the fridge
        public virtual ICollection<FridgeFault> Faults { get; set; }

        // Navigation property for maintenance records associated with the fridge
        public virtual ICollection<FridgeMaintenance> Maintenances { get; set; }

        // Navigation property for allocation records of the fridge
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }

        // Navigation property for service history entries of the fridge
        public virtual ICollection<FridgeServiceHistory> ServiceHistory { get; set; }
    }
}
