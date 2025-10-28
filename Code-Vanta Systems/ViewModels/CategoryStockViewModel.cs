namespace Code_Vanta_Systems.ViewModels
{
    public class CategoryStockViewModel
    {
        public string Category { get; set; }
        public int TotalStock { get; set; }
        public int LowStock { get; set; }
        public int MediumStock { get; set; }
        public int HighStock { get; set; }
        public int CriticalStock { get; set; }

        public string StockStatus(int qty)
        {
            if (qty < 50) return "Critically Low";
            if (qty < 100) return "Low";
            if (qty < 200) return "Medium";
            return "High";
        }
    }
}
