using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int Id { get; set; }

        // Date when the request was made
        public DateTime RequestDate { get; set; }

        // Foreign key referencing the user who made the request
        [Required]  
        public string RequestedByUserId { get; set; }

        // Navigation property for the requesting user
        public virtual ApplicationUser RequestedBy { get; set; }

        // Model of the fridge or item being requested
        [Required] 
        [StringLength(100)]
        public string ModelRequested { get; set; }

        // Quantity of items requested
        [Required]
        public int Quantity { get; set; }

        // Status of the request (e.g., Pending, Approved, Rejected)
        [Required]
        public RequestStatus Status { get; set; }

        // Collection of quotations linked to this request
        public virtual ICollection<Quotation> Quotations { get; set; }
    }
}
