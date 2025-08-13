using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string BusinessName { get; set; }

        [Required, StringLength(100)]
        public string ContactName { get; set; }

        [Required, StringLength(100)]
        public string Phone { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // Navigation
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }

        public ICollection<FridgeRequest> FridgeRequests { get; set; }

    }
}
