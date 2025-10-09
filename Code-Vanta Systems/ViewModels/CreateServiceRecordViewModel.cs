using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateServiceRecordViewModel
    {
        public int FridgeId { get; set; }
        public string SerialNumber { get; set; }
        public int? ScheduleId { get; set; } // Optional: Link to a schedule being completed

        [Required]
        [Display(Name = "Date Performed")]
        [DataType(DataType.Date)]
        public DateTime DatePerformed { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Service Report/Actions Taken")]
        [DataType(DataType.MultilineText)]
        public string ReportDetails { get; set; }

        [Display(Name = "Parts Used")]
        [DataType(DataType.MultilineText)]
        public string? PartsUsed { get; set; }

        [Display(Name = "Date of Service")]
        [Required(ErrorMessage = "Service date is required.")]
        [DataType(DataType.Date)]
        public DateTime DateOfService { get; set; } 

        [Display(Name = "Service Details")]
        [Required(ErrorMessage = "Please enter the details of the service performed.")]
        [StringLength(500, ErrorMessage = "Details cannot exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string ServiceDetails { get; set; } 

        [Display(Name = "Fault Resolved (Optional)")]
        public int? FaultId { get; set; }

        public string? TechnicianId { get; set; }
    }
}
