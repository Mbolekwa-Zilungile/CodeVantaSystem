using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class Quotation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateReceived { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Unit Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Please enter a valid amount with up to 2 decimal places")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        [Required]
        public int PurchaseRequestId { get; set; }
        public virtual PurchaseRequest PurchaseRequest { get; set; }

        // Optional link to RFQ
        public int? RequestQuotationId { get; set; }
        public virtual RequestQuotation RequestQuotation { get; set; }
    }
}
