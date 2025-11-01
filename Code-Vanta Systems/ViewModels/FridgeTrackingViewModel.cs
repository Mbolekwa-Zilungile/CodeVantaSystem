namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeTrackingViewModel
    {
        public string FridgeModel { get; set; }
        public string? SerialNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? PhysicalAddress { get; set; }
        public string CustomerAddress { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } // "Approved Request" or "Allocated"
        public string? AllocatedBy { get; set; }
        public DateTime AllocationDate { get; set; }
        public DateTime? Date { get; set; }

        // Add these if needed
        public int? FridgeUnitId { get; set; }
        public string? Category { get; set; }
        public string? CustomerEmail { get; set; }
    }
}
