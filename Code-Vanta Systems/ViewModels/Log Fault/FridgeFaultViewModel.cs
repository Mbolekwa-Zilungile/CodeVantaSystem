using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Log_Fault
{
    public class FridgeFaultViewModel
    {
        public int Id { get; set; }

        // Fridge Info
        public int FridgeId { get; set; }
        public string FridgeModel { get; set; }
        // CRITICAL FIX: Add FridgeUnitId for history links
        public int FridgeUnitId { get; set; }
        public string FridgeSerialNumber { get; set; }

        // Reporter Info
        public string ReportedByUserId { get; set; }
        public string ReportedByFullName { get; set; }

        // Technician Info (optional)
        public string? AssignedTechnicianId { get; set; }
        public string? AssignedTechnicianName { get; set; }

        [Required]
        [Display(Name = "Date Reported")]
        public DateTime DateReported { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Fault Description")]
        public string FaultDescription { get; set; }

        [Required]
        public FaultStatus Status { get; set; }

        [Display(Name = "Resolution Date")]
        public DateTime? ResolutionDate { get; set; }

        // Optional: Display-friendly status text
        public string StatusText => Status.ToString();
    }
}
