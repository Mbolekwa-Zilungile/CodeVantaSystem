// PurchaseRequestDetailsViewModel.cs
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.PurchaseRequest;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class PurchaseRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; }
        public DateTime RequestDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }

        public RequestStatus Status { get; set; }
        public PriorityLevel Priority { get; set; }
        public string? Notes { get; set; }

        public string RequestedByName { get; set; }
        public string RequestedByEmail { get; set; }

        public List<PurchaseRequestItemDetailsViewModel> Items { get; set; } = new();
    }
}