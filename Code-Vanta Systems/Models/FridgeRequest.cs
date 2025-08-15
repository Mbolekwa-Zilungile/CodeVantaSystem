using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeRequest
    {

        public int Id { get; set; }

        // Foreign key referencing the customer making the request
        [Required]
        public int CustomerId { get; set; }

        // Navigation property for the customer
        public Customer Customer { get; set; }

        // Foreign key referencing the location of the request
        [Required]
        public int LocationId { get; set; }

        // Navigation property for the location
        public Location Location { get; set; }


        // Timestamp when the request was made
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Optional notes or remarks about the request
        public string Notes { get; set; }

        // Status of the request (e.g., Pending, Approved, Rejected)
        public RequestStatus Status { get; set; }

    }
}
