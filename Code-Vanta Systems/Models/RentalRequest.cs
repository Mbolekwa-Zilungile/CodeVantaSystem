using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RentalRequest
    {
        public int Id { get; set; }
        public int FridgeId { get; set; }
        public Fridge Fridge { get; set; }

        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending";

        public string RequestedByUserId { get; set; }
        public ApplicationUser RequestedByUser { get; set; }

        public int? AllocationId { get; set; }  
        public FridgeAllocation Allocation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }


}
