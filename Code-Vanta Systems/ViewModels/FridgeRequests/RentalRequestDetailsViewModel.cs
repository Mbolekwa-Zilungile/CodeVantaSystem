using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.FridgeRequests
{
    public class RentalRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string FridgeModelName { get; set; }
        public FridgeCategory FridgeModelCategory { get; set; }
        public int Quantity { get; set; }
        public int RequestedDuration { get; set; }
        public string SpecialRequirements { get; set; }
        public string Status { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal MonthlyCost { get; set; }
        public decimal TotalCost => MonthlyCost * RequestedDuration;
    }
}
