using System;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class ScheduleRepairVisitViewModel
    {
        [Required]
        public int FaultId { get; set; }

        [Required(ErrorMessage = "The scheduled date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Visit Date & Time")]
        public DateTime ScheduledDateTime { get; set; }

        // Fields for display context in the view (populated by the GET action)
        public string UnitSerial { get; set; }
        public string CustomerName { get; set; }
    }
}