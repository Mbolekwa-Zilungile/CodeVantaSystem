using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.Maintenance_Schedule
{
    public class CreateMaintenanceViewModel
    {
        [Required]
        [Display(Name = "Fridge")]
        public int FridgeId { get; set; }

        [Required]
        [Display(Name = "Scheduled Date")]
        public DateTime MaintenanceDate { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Maintenance Description")]
        public string MaintenanceDescription { get; set; }

        [Required]
        [Display(Name = "Status")]
        public MaintenanceStatus Status { get; set; }
    }
}
