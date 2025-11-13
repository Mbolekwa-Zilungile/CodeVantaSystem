using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RentalRequest
    {
        public int Id { get; set; }

        // OLD: FridgeId -> NEW: FridgeModelId
        public int FridgeModelId { get; set; }
        public FridgeModel FridgeModel { get; set; }

        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending";

        public string RequestedByUserId { get; set; }
        public ApplicationUser RequestedByUser { get; set; }

        // The allocation is for a unit, but the request is for a model, 
        // this link is maintained to track fulfillment.
        public int? AllocationId { get; set; }
        public FridgeAllocation Allocation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsPaid { get; set; } = false;
    }
}