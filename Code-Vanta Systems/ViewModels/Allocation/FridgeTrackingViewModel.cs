namespace Code_Vanta_Systems.ViewModels.Allocation
{
    public class FridgeTrackingViewModel
    {
        public string FridgeModel { get; set; }
        public string SerialNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public int Quantity { get; set; }
        public string AllocatedBy { get; set; }
        public DateTime AllocationDate { get; set; }
        public string Status { get; set; }
    }
}
