using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class MaintenanceFridgeSummaryViewModel
    {
        public int FridgeId { get; set; }

        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        public string Model { get; set; }

        [Display(Name = "Status")]
        public string FridgeStatus { get; set; }

        public string Category { get; set; }

        [Display(Name = "Customer")]
        public string CustomerBusinessName { get; set; }

        [Display(Name = "Customer Address")]
        public string CustomerAddress { get; set; }

        [Display(Name = "Location")]
        public string LocationDescription { get; set; }

        [Display(Name = "Supplier")]
        public string SupplierName { get; set; }
    }
}