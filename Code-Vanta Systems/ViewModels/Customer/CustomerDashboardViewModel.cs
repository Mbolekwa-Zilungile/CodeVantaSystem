namespace Code_Vanta_Systems.ViewModels.Customer
{
    public class CustomerDashboardViewModel
    {
        // --- Summary Cards ---
        public int TotalLogged { get; set; }
        public int TotalRequested { get; set; }
        public int TotalPending { get; set; }
        public int? TotalAllocated { get; set; }

        // --- Charts ---
        public List<ChartDataPoint> FaultsOverTime { get; set; } = new();
    }
}
