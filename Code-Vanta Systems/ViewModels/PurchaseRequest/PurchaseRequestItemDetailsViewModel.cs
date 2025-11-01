namespace Code_Vanta_Systems.ViewModels.PurchaseRequest
{
    public class PurchaseRequestItemDetailsViewModel
    {
        public int FridgeModelId { get; set; }
        public string FridgeModel { get; set; }
        public string SerialNumber { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal EstimatedUnitCost { get; set; }
        public decimal TotalEstimatedCost => Quantity * EstimatedUnitCost;
    }
}
