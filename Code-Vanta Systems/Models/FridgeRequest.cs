using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeRequest
    {

        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        [Required]
        public int LocationId { get; set; }
        public Location Location { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string Notes { get; set; }

        public RequestStatus Status { get; set; }

    }
}
