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

        [Required]
        [StringLength(50)]
        public string PONumber { get; set; }

        [Required]
        public int QuotationId { get; set; }
        public Quotation Quotation { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [MaxLength(200)]
        public string DeliveryAddress { get; set; }

        [MaxLength(200)]
        public string PaymentTerms { get; set; }

        [MaxLength(100)]
        public string DeliveryTerms { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public string OrderedByUserId { get; set; }
        public ApplicationUser OrderedBy { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public ICollection<DeliveryNote> DeliveryNotes { get; set; } = new HashSet<DeliveryNote>();


        [NotMapped]
        public decimal TotalAmount => Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
    }
}
