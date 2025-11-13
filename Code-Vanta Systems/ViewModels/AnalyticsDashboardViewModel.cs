using Code_Vanta_Systems.Controllers;
using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class AnalyticsDashboardViewModel
    {
        public List<StatusCount> FridgeStatus { get; set; } = new();
        public List<StatusCount> FridgeRegion { get; set; } = new();
        public List<CategoryStockViewModel> FridgeCategories { get; set; } = new();
        public List<StatusCount> OrderStatus { get; set; } = new();
        public List<StatusCount> CustomerStatus { get; set; } = new();
        public List<AllocationTrend> Allocations { get; set; } = new();
        public List<FridgeUnit> AllFridges { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
