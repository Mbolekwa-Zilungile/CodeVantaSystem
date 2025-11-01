
using Code_Vanta_Systems.ViewModels.Shared;

namespace Code_Vanta_Systems.ViewModels.Customer
{
    public class CustomerDashboardViewModel
    {
        // --- Summary Cards ---
        public int TotalLogged { get; set; }
        public int TotalRequested { get; set; }
        public int TotalPending { get; set; }
        public int? TotalAllocated { get; set; }
        public int RentalsCount { get; set; }
        public int PendingOrders { get; set; }
        public int PaidOrders { get; set; }
        public int UnPaidOrders { get; set; }
        public int RejectedOrders { get; set; }
        public int ApprovedOrders { get; set; }
        // --- Charts ---
        public List<ChartDataPoint> FaultsOverTime { get; set; } = new();
    }
}
