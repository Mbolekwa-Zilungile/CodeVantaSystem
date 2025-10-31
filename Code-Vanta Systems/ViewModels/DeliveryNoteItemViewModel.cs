namespace Code_Vanta_Systems.ViewModels
{
    public class DeliveryNoteItemViewModel
    {
        public int PurchaseOrderId { get; set; }
        public string PONumber { get; set; }

        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierAddress { get; set; }

        public string DeliveryAddress { get; set; }
        public string DeliveryTerms { get; set; }
        public string PaymentTerms { get; set; }

        public DateTime? GeneratedDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; }

        public List<DeliveryNoteItemViewModel> Items { get; set; } = new();
    }
}
