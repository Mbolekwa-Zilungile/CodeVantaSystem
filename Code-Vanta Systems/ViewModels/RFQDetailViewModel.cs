using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class RFQDetailViewModel
    {
        public string RFQId { get; set; }
        public int RequestQuotationId { get; set; }
        public DateTime RequestDate { get; set; }
        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string RequestedBy { get; set; }
        public string Notes { get; set; }

        public ICollection<QuotationItem> Items { get; set; }
    }
}
