namespace Code_Vanta_Systems.ViewModels.Analytics
{
    public class PerformanceMetricsViewModel
    {
        public int TotalAllocations { get; set; }
        public int MonthlyAllocations { get; set; }
        public double AllocationSuccessRate { get; set; }
        public double CustomerSatisfactionScore { get; set; }
        public double AverageAllocationDuration { get; set; }
        public double InventoryUtilizationRate { get; set; }
        public int LowStockModelsCount { get; set; }
    }
}
