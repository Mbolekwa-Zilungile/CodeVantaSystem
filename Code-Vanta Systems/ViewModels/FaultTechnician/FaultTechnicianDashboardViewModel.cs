using Code_Vanta_Systems.ViewModels.Customer;
using Code_Vanta_Systems.ViewModels.Shared;

namespace Code_Vanta_Systems.ViewModels.FaultTechnician
{
    public class FaultTechnicianDashboardViewModel
    {
        public int TotalAssignedFaults { get; set; }
        public int TotalPendingFaults { get; set; }
        public int TotalCompletedFaults { get; set; }
        public List<ChartDataPoint> FaultsOverTime { get; set; }
        public List<RecentFaultViewModel> RecentFaults { get; set; }
    }
}
