using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; }

        [Required, StringLength(200)]
        public string Address { get; set; }

        [Required, StringLength(200)]
        public string City { get; set; }

        [Required, StringLength(200)]
        public string Region { get; set; }

        // Navigation
        public virtual ICollection<Customer> Customers { get; set; }
        public virtual ICollection<Fridge> Fridges { get; set; }

        public ICollection<FridgeRequest> FridgeRequests { get; set; }

    }
}
