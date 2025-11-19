using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels
{
    public class TechnicianDashboardViewModel
    {
        // Summary Cards
        public int TotalAssignedFaults { get; set; }
        public int TotalPendingFaults { get; set; }
        public int TotalCompletedFaults { get; set; }
        public int TotalScrappedUnits { get; set; }
        public int ReplacementRequests { get; set; }
        public double AverageResolutionTimeHours { get; set; }

        // Charts Data
        public List<ChartDataPoint> FaultsOverTime { get; set; }
        public List<ChartDataPoint> StatusDistribution { get; set; }
        public List<ChartDataPoint> SeverityDistribution { get; set; }
        public List<ChartDataPoint> MonthlyPerformance { get; set; }

        // Recent Activity
        public List<RecentFault> RecentFaults { get; set; }
        public List<PerformanceMetric> PerformanceMetrics { get; set; }

        // Business Insights
        public string TopIssueType { get; set; }
        public int CurrentMonthCompleted { get; set; }
        public int PreviousMonthCompleted { get; set; }
        public double CompletionRate { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string Color { get; set; }
    }

    public class RecentFault
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Customer { get; set; }
        public string Status { get; set; }
        public string Severity { get; set; }
        public DateTime DateReported { get; set; }
        public int DaysOpen { get; set; }
    }

    public class PerformanceMetric
    {
        public string Metric { get; set; }
        public string Value { get; set; }
        public string Trend { get; set; } // "up", "down", "stable"
        public string TrendValue { get; set; }
    }
}