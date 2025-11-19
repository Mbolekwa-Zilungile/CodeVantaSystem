using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FaultTechnician
{
    public class ReplacementBookingViewModel
    {
        [Required]
        public int FaultId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Display(Name = "Additional Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        // Display properties
        public string CustomerName { get; set; }
        public string LocationName { get; set; }
        public string ScrappedUnitSerial { get; set; }
    }
}