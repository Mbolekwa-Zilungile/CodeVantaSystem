using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.PurchaseOrder
{
    public class PurchaseOrderCreateViewModel
    {
        public int QuotationId { get; set; }
        public string QuotationNumber { get; set; } = "";
        public int SupplierId { get; set; }

        // Supplier Info
        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierAddress { get; set; }

        // Company Info
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }

        // Manager / Authorized By Info
        public string ManagerName { get; set; }
        public string ManagerEmail { get; set; }
        public string ManagerRole { get; set; }

        // Purchase Order Details
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Required]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; }

        [Display(Name = "Delivery Terms")]
        public string DeliveryTerms { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        // Items included in the Purchase Order
        public List<PurchaseOrderItemViewModel> Items { get; set; } = new List<PurchaseOrderItemViewModel>();
    }
}
