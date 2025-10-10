namespace Code_Vanta_Systems.ViewModels
{
    public class RFQListItemViewModel
    {
        public int RequestQuotationId { get; set; } // ✅ Required for linking/viewing details
        public string RFQId { get; set; }
        public DateTime RequestDate { get; set; }
        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string RequestedBy { get; set; }
        public string Notes { get; set; }
    }
}
