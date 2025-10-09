using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class RentalViewModel
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }
        public string ImageUrl { get; set; }
        public decimal PricePerMonth { get; set; }
        public int StockQuantity { get; set; }
        public int Quantity { get; set; } = 0;
        public string Location { get; set; }
    }

}
