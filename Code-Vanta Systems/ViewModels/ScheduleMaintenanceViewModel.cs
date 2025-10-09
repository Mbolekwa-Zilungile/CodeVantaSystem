using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class ScheduleMaintenanceViewModel
    {
        public int FridgeId { get; set; }
        public string SerialNumber { get; set; }
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Scheduled Date & Time")]
        [DataType(DataType.DateTime)]
        public DateTime ScheduledDate { get; set; } = DateTime.Now.AddDays(1);

        [Required]
        [Display(Name = "Maintenance Type/Description")]
        [StringLength(500)]
        public string Description { get; set; }

        public string? TechnicianId { get; set; }
    }
}
