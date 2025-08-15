using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class Quotation
    {
        [Key]
        public int Id { get; set; }

        // Date when the quotation was received
        [Required]
        public DateTime DateReceived { get; set; }

        // Unit price quoted by the supplier
        [Required(ErrorMessage = "Unit Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Please enter a valid amount with up to 2 decimal places")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Foreign key referencing the supplier who provided the quotation
        [Required]
        public int SupplierId { get; set; }

        // Navigation property for the supplier
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        // Foreign key referencing the related purchase request
        [Required]
        public int PurchaseRequestId { get; set; }

        // Navigation property for the purchase request
        [ForeignKey("PurchaseRequestId")]
        public virtual PurchaseRequest PurchaseRequest { get; set; }
    }
}
