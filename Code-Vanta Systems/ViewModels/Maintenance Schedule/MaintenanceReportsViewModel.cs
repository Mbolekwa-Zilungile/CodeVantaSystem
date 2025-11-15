using Code_Vanta_Systems.ViewModels.Customer; // Assuming ChartDataPoint is defined here
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class MaintenanceReportsViewModel
    {
        [Display(Name = "Total Assigned Faults")]
        public int TotalAssignedFaults { get; set; }

        [Display(Name = "Total Repaired/Resolved Faults")]
        public int TotalRepairedFaults { get; set; }

        [Display(Name = "Total Upcoming Schedules")]
        public int TotalScheduledMaintenance { get; set; }

        // Data structure for the Fault Activity Over Time chart
        public List<ChartDataPoint> ActivityOverTime { get; set; } = new List<ChartDataPoint>();
    }
}