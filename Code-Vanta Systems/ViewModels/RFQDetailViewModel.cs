using Code_Vanta_Systems.Models;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RFQDetailViewModel
    {
        public int RequestQuotationId { get; set; }
        public string RFQId { get; set; }
        public DateTime RequestDate { get; set; }

        [Display(Name = "Expected Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }
        public string Notes { get; set; }
        public string IssuedByName { get; set; }

        // Company Info
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyAddress { get; set; }

        public string? SupplierName { get; set; }
        public string? SupplierEmail { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierAddress { get; set; }
        public string PurchaseRequestNumber { get; set; }
        public string RequestedBy { get; set; }
        public List<RFQItemViewModel> Items { get; set; } = new List<RFQItemViewModel>();

        public List<QuotationSummaryViewModel> SubmittedQuotations { get; set; } = new List<QuotationSummaryViewModel>();
    }
}
