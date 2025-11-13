using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class QuotationItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuotationId { get; set; }

        // Change: FridgeId -> FridgeModelId
        public int FridgeModelId { get; set; }
        // Change: FridgeModel property to be clear
        public string FridgeModelName { get; set; }
        // Quantity property is fine here for the request quantity
        public int Quantity { get; set; }
        public virtual Quotation Quotation { get; set; }

        [Required]
        public int PurchaseRequestItemId { get; set; }
        public virtual PurchaseRequestItem PurchaseRequestItem { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuotedUnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuotedQuantity { get; set; }
        public int WarrantyMonths { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
    }
}