using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Admin
{
    public class ReplacementRequestViewModel
    {
        public int RequestId { get; set; }
        public int FaultId { get; set; }
        public string ScrappedUnitSerial { get; set; }
        public string CustomerName { get; set; }
        public string LocationName { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public string Notes { get; set; }
        public RequestStatus Status { get; set; }
        public string ScrappedFridgeModel { get; set; }
        public DateTime ScrappedDate { get; set; }
    }
}