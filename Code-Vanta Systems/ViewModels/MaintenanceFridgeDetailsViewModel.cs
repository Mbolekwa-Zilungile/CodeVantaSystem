using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class MaintenanceFridgeDetailsViewModel
    {
        public MaintenanceFridgeListViewModel FridgeSummary { get; set; }
        public ICollection<ServiceRecord> ServiceHistory { get; set; }
        public ICollection<FridgeFault> FaultHistory { get; set; }
        public ICollection<MaintenanceSchedule> UpcomingSchedules { get; set; }
        public List<UnitDetailViewModel> UnitDetails { get; set; }

    }
}
