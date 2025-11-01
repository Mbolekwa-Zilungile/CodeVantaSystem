using Code_Vanta_Systems.Models;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeAllocation
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
        public DateTime AllocationDate { get; set; }

        public DateTime? DeallocationDate { get; set; }

        // REMOVE: Quantity field (now 1:1 relationship)
        // [Required]
        // [Range(1, int.MaxValue)]
        // public int Quantity { get; set; }

        public string CreatedByUserId { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }
    }
}