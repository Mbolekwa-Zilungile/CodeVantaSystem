using Code_Vanta_Systems.Models.Enums;
using System.Collections.Generic;

namespace Code_Vanta_Systems.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStock { get; set; }
        public int DeletedStock { get; set; }
        public Dictionary<FridgeStatus, int> StatusCounts { get; set; }
        public List<GroupedStockViewModel> GroupedStock { get; set; }
    }

    public class GroupedStockViewModel
    {
        public string Category { get; set; }
        public string Model { get; set; }
        public int Count { get; set; }
        public string StockLevel { get; set; } // Low / Average / High
    }
}
