namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeTrackingViewModel
    {
        public string FridgeModel { get; set; }
        public string? SerialNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? PhysicalAddress { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } // "Approved Request" or "Allocated"
        public string? AllocatedBy { get; set; }
        public DateTime? Date { get; set; }
    }
}
