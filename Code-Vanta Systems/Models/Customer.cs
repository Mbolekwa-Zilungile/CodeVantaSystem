using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        // Registered business name of the customer
        [Required]
        [StringLength(100)]
        public string BusinessName { get; set; }

        // Primary contact person's name
        [Required]
        [StringLength(100)]
        public string ContactName { get; set; }

        // Contact phone number (validated format)
        [Required]
        [StringLength(100), Phone]
        public string Phone { get; set; }

        // Contact email address (validated format)
        [Required] 
        [StringLength(100), EmailAddress]
        public string Email { get; set; }

        // Foreign key referencing the customer's location
        [Required]
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // Navigation property for fridge allocations linked to this customer
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }

        // Navigation property for fridge requests made by this customer
        public ICollection<FridgeRequest> FridgeRequests { get; set; }

        // Constructor initializes collections to prevent null reference issues
        public Customer()
        {
            Allocations = new HashSet<FridgeAllocation>();
            FridgeRequests = new HashSet<FridgeRequest>();
        }

    }
}
