namespace Code_Vanta_Systems.ViewModels.SupplierQoutation
{
    public class SupplierQuotationCreateViewModel
    {
        public int RFQId { get; set; }
        public string RFQCode { get; set; }
        public int PurchaseRequestId { get; set; }

        // Add this property
        public DateTime RequestDate { get; set; }

        public string IssuedByName { get; set; }
        public string? Notes { get; set; }

        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }

        public DateTime? ExpectedDate { get; set; }

        public List<SupplierQuotationItemViewModel> Items { get; set; } = new();
    }
}
