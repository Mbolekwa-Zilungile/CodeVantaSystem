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

        // OLD: Fridges -> NEW: FridgeUnits
        public virtual ICollection<FridgeUnit> FridgeUnits { get; set; }

        // Navigation property for fridge requests made from this location
        public virtual ICollection<FridgeRequest> FridgeRequests { get; set; }

        // Navigation property for employees based at this location
        public virtual ICollection<Employee> Employees { get; set; }

        // Navigation property for scheduled technician visits
        public virtual ICollection<TechnicianSchedule> ScheduledVisits { get; set; }


        public Location()
        {
            Customers = new HashSet<Customer>();
            FridgeUnits = new HashSet<FridgeUnit>(); // Updated initialization
            FridgeRequests = new HashSet<FridgeRequest>();
            Employees = new HashSet<Employee>();
            ScheduledVisits = new HashSet<TechnicianSchedule>();
        }
    }
}