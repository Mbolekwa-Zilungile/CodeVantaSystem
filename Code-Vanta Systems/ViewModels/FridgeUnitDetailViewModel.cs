namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeUnitDetailViewModel
    {
        public int UnitId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AllocatedToCustomer { get; set; } = string.Empty;
    }
}
