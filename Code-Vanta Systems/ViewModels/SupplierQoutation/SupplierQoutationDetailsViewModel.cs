namespace Code_Vanta_Systems.ViewModels.SupplierQoutation
{
    public class SupplierQoutationDetailsViewModel
    {

        public int QuotationId { get; set; }
        public string RFQCode { get; set; }
        public DateTime RequestDate { get; set; }
        public string Notes { get; set; }
        public string IssuedByName { get; set; }

        // Company info
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }
        public DateTime? ExpectedDate { get; set; }
        // Items
        public List<SupplierQuotationItemViewModel> Items { get; set; } = new();
    }

}