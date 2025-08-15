using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeAllocation
    {
        [Key]
        public int Id { get; set; }

        // Foreign key referencing the allocated fridge
        public int FridgeId { get; set; }

        // Navigation property for the fridge
        public virtual Fridge Fridge { get; set; }

        // Foreign key referencing the customer receiving the fridge
        public int CustomerId { get; set; }

        // Navigation property for the customer
        public virtual Customer Customer { get; set; }

        // Date when the fridge was allocated to the customer
        public DateTime AllocationDate { get; set; }

        // Optional date when the fridge was deallocated or returned
        public DateTime? DeallocationDate { get; set; }
    }
}
