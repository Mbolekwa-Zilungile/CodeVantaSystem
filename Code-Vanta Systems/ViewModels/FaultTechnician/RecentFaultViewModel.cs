namespace Code_Vanta_Systems.ViewModels.FaultTechnician
{
    public class RecentFaultViewModel
    {
        public int Id { get; set; }
        public string FridgeModel { get; set; }
        public string SerialNumber { get; set; }
        public string Status { get; set; }
        public string ReportedBy { get; set; }
        public DateTime DateReported { get; set; }
    }
}
