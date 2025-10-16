namespace Code_Vanta_Systems.ViewModels
{
    public class MaintenanceFridgeListViewModel
    {
        public int FridgeId { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string FridgeStatus { get; set; }

        public int CustomerId { get; set; }
        public string CustomerBusinessName { get; set; }
        public string CustomerAddress { get; set; } // Using Customer's address

        public DateTime AllocationDate { get; set; }
        public DateTime? NextScheduledVisit { get; set; }
    }
}
