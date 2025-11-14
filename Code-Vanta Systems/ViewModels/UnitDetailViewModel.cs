namespace Code_Vanta_Systems.ViewModels
{
    // Location: Code_Vanta_Systems.ViewModels (or wherever your ViewModels are stored)

    public class UnitDetailViewModel
    {
        public int UnitId { get; set; }
        public string SerialNumber { get; set; }
        public string Condition { get; set; }
        public string UnitStatus { get; set; }
        public string CurrentCustomer { get; set; }
        public string AssignedTechnician { get; set; }

        // You would eventually add ICollection<FridgeFault> FaultHistory { get; set; } here
    }
}
