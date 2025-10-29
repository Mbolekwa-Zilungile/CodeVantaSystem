using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.SupplierQoutation
{
    public class SupplierQuotationItemViewModel
    {
        public int PurchaseRequestItemId { get; set; }
        public int FridgeId { get; set; }
        public string FridgeModel { get; set; }
        public int RequestedQuantity { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int QuotedQuantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal QuotedUnitPrice { get; set; }

        [Required]
        [Display(Name = "Warranty (months)")]
        public int WarrantyMonths { get; set; }

       
        [Display(Name = "Estimated Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime ExpectedDeliveryDate { get; set; }
    }
}