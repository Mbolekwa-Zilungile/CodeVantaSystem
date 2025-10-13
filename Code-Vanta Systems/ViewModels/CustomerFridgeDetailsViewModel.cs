using Code_Vanta_Systems.Models;
using System.Collections.Generic;

namespace Code_Vanta_Systems.ViewModels
{
    public class CustomerFridgeDetailsViewModel
    {
        // Summary of the fridge for display (reusing Technician's list item logic)
        public MaintenanceFridgeListViewModel FridgeSummary { get; set; }

        // History: Service Records (Completed maintenance)
        public ICollection<ServiceRecord> ServiceHistory { get; set; } = new List<ServiceRecord>();

        // History: Fault Reports (Open or closed faults)
        public ICollection<FridgeFault> FaultHistory { get; set; } = new List<FridgeFault>();

        // Upcoming: Scheduled visits
        public ICollection<MaintenanceSchedule> UpcomingSchedules { get; set; } = new List<MaintenanceSchedule>();
    }
}