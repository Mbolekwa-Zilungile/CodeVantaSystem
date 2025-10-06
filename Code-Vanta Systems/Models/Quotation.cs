using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    // Quotation is now a header that contains no line-item details.
    public class Quotation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateReceived { get; set; } = DateTime.Now;

        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        [Required]
        public int PurchaseRequestId { get; set; }
        public virtual PurchaseRequest PurchaseRequest { get; set; }

        // Optional link to RFQ
        public int? RequestQuotationId { get; set; }
        public virtual RequestQuotation RequestQuotation { get; set; }

        // Navigation collection for the line items of the quotation
        public virtual ICollection<QuotationItem> Items { get; set; } = new HashSet<QuotationItem>();
    }
}