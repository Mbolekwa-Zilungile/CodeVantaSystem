using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseOrder
    {
        [Key]
        public int PurchaseOrderId { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Now;
        [ForeignKey("Quotation")]
        public int? QuotationId { get; set; } // Nullable, as POs might not always come from a quote
        public virtual Quotation Quotation { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public string OrderedByUserId { get; set; }
        public ApplicationUser OrderedBy { get; set; }


        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ICollection<PurchaseOrderItem> Items { get; set; } = new HashSet<PurchaseOrderItem>();

        // New: Navigation property for delivery notes
        public ICollection<DeliveryNote> DeliveryNotes { get; set; } = new HashSet<DeliveryNote>();

        [NotMapped]
        public decimal TotalAmount => Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;

        public string DeliveryAddress { get; set; }
        public string DeliveryTerms { get; set; }
        public string PaymentTerms { get; set; }
    }
}
