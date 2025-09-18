using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class Location
    {
        public int Id { get; set; }

        // Description of the location (e.g., branch name or identifier)
        [Required, StringLength(200)]
        public string Description { get; set; }

        // Physical address of the location
        [Required, StringLength(200)]
        public string Address { get; set; }

        // City where the location is situated
        [Required, StringLength(200)]
        public string City { get; set; }

        // Province (formerly Region) of the location
        [Required]
        public Region Province { get; set; }

        // Navigation property for customers associated with this location
        public virtual ICollection<Customer> Customers { get; set; }

        // Navigation property for fridges located at this location
        public virtual ICollection<Fridge> Fridges { get; set; }

        // Navigation property for fridge requests made from this location
        public virtual ICollection<FridgeRequest> FridgeRequests { get; set; }

        public Location()
        {
            Customers = new HashSet<Customer>();
            Fridges = new HashSet<Fridge>();
            FridgeRequests = new HashSet<FridgeRequest>();
        }
    }
}
