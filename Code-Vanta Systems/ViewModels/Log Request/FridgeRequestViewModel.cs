using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Log_Request
{
    public class FridgeRequestViewModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        public int LocationId { get; set; }
        public string LocationName { get; set; }

        public string RequestedByUserId { get; set; }
        public string RequestedByFullName { get; set; }

        public DateTime RequestedAt { get; set; }
        public string Notes { get; set; }
        public RequestStatus Status { get; set; }

        // Display-friendly status
        public string StatusText => Status.ToString();
    }
}
