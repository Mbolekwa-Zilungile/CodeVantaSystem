namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeTrackingViewModel
    {
        public string FridgeModel { get; set; }             // Fridge model
        public string? UnitSerialNumber { get; set; }       // Fridge unit serial
        public string? CustomerName { get; set; }           // Customer business name
        public string? CustomerAddress { get; set; }        // Customer physical address
        public string? CustomerLocation { get; set; }       // Customer location name
        public string? Status { get; set; }                 // e.g., "Allocated", "Approved Request"
        public string? Condition { get; set; }              // Fridge unit condition
        public DateTime AllocatedAt { get; set; }
    }
}