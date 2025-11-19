namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeModelStockViewModel
    {
        public int FridgeId { get; set; }
        public string Model { get; set; } = string.Empty;
        public int TotalStockQuantity { get; set; }
        public int AvailableUnits { get; set; }
    }
}
