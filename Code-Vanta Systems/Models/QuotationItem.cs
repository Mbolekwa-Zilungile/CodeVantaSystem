using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    // This model holds the line-item details for a specific Quotation.
    public class QuotationItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuotationId { get; set; }
        public virtual Quotation Quotation { get; set; }

        // Link back to the original PurchaseRequestItem being quoted
        [Required]
        public int PurchaseRequestItemId { get; set; }
        public virtual PurchaseRequestItem PurchaseRequestItem { get; set; }

        // The actual price quoted by the supplier (transactional price)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedUnitPrice { get; set; }

        // The Quantity the supplier quoted (should match the request quantity, but might differ)
        [Required]
        [Range(1, int.MaxValue)]
        public int QuotedQuantity { get; set; }
    }
}