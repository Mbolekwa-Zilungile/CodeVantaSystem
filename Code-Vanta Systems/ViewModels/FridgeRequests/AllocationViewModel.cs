namespace Code_Vanta_Systems.ViewModels.FridgeRequests
{
    public class AllocationViewModel
    {
        public int Id { get; set; }
        public string FridgeModelName { get; set; }
        public string SerialNumber { get; set; }
        public DateTime AllocationDate { get; set; }
        public DateTime? DeallocationDate { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
    }
}
