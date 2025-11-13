using System;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.Models
{
    // Tracks the deployment of a single FridgeUnit to a Customer.
    public class FridgeAllocation
    {
        [Key]
        public int Id { get; set; }

        // NEW: Foreign key to the specific physical unit
        [Required]
        public int FridgeUnitId { get; set; }
        public virtual FridgeUnit FridgeUnit { get; set; } // New navigation property

        [Required]
        public int CustomerId { get; set; } // Retained from FridgeAllocation.cs
        public virtual Customer Customer { get; set; } // Retained from FridgeAllocation.cs

        [Required]
        public DateTime AllocationDate { get; set; } // Retained from FridgeAllocation.cs

        public DateTime? DeallocationDate { get; set; } // Retained from FridgeAllocation.cs

        // NOTE: Removed 'Quantity' as this now represents one physical unit.

        public string CreatedByUserId { get; set; } // Retained from FridgeAllocation.cs
        public virtual ApplicationUser CreatedBy { get; set; } // Retained from FridgeAllocation.cs

        // FridgeAllocation.cs
        public bool IsActive { get; set; } = true; // <--- ADD THIS LINE (if you prefer the bool property)
                                               
    }
}