using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class RFQDetailViewModel
    {
        public int RequestQuotationId { get; set; }
        public string RFQId { get; set; }
        public DateTime RequestDate { get; set; }
        public string Notes { get; set; }
        public string IssuedByName { get; set; }

        // Company Info
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }

        public List<RFQItemViewModel> Items { get; set; } = new List<RFQItemViewModel>();

        public List<QuotationSummaryViewModel> SubmittedQuotations { get; set; } = new List<QuotationSummaryViewModel>();
    }
}
