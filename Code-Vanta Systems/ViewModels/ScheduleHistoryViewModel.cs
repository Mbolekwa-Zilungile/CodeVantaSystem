using System;

namespace Code_Vanta_Systems.ViewModels
{
    public class ScheduleHistoryViewModel
    {
        public DateTime ScheduledDate { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Assuming Status is a string (e.g., "Pending", "Complete")
    }
}