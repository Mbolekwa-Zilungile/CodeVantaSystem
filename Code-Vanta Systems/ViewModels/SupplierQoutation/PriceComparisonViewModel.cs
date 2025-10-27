using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels.SupplierQoutation
{
    public class PriceComparisonViewModel
    {
            public int PurchaseRequestId { get; set; }
            public List<Quotation> Quotations { get; set; }

    }
}
