using Code_Vanta_Systems.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string BusinessName { get; set; }

        [Required, StringLength(100)]
        public string ContactName { get; set; }

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; }

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; }

        // Business Type (e.g., Spaza Shop, Shebeen)
        [Required, StringLength(50)]
        public string BusinessType { get; set; }

        // Physical Address
        [StringLength(250)]
        public string PhysicalAddress { get; set; }

        // Location where customer is based
        [Required]
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // 📜 Business Verification Document
        // (Path to uploaded file - e.g. PDF, image, etc.)
        [StringLength(255)]
        public string? BusinessDocumentPath { get; set; }


        public CustomerStatus Status { get; set; }
        public bool IsActive { get; set; } = true;

        // Optional link to an ApplicationUser (for customer portal logins)
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Foreign Key property for the assigned technician
        public string? TechnicianId { get; set; }
        public virtual ApplicationUser? Technician { get; set; }

        // Navigation collections
        public virtual ICollection<FridgeAllocation> Allocations { get; set; }
        public virtual ICollection<FridgeRequest> FridgeRequests { get; set; }

        public Customer()
        {
            Allocations = new HashSet<FridgeAllocation>();
            FridgeRequests = new HashSet<FridgeRequest>();
        }
    }
}
