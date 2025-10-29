namespace Code_Vanta_Systems.ViewModels.PurchaseOrder
{
    public class PurchaseOrderCreateViewModel
    {
        public int QuotationId { get; set; }

        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierAddress { get; set; }

        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }

        public string ManagerName { get; set; }
        public string ManagerEmail { get; set; }

        public string ManagerRole { get; set; }

        public DateTime OrderDate { get; set; }
        public string DeliveryAddress { get; set; }
        public string DeliveryTerms { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }

        public List<PurchaseOrderItemViewModel> Items { get; set; } = new();
    }
}
