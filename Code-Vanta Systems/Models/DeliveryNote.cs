using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class DeliveryNote
    {
        [Key]
        public int Id { get; set; }

        // Date when the delivery was made
        [Required]
        public DateTime DeliveryDate { get; set; }

        // Foreign key referencing the supplier who made the delivery
        [Required]
        public int SupplierId { get; set; }

        // Navigation property for the supplier
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        // Foreign key referencing the related purchase request
        [Required]
        public int PurchaseRequestId { get; set; }

        // Navigation property for the associated purchase request
        [ForeignKey("PurchaseRequestId")]
        public virtual PurchaseRequest PurchaseRequest { get; set; }

        // Optional notes related to the delivery (e.g., condition, remarks)
        [StringLength(500)]
        public string Notes { get; set; }
    }
}
