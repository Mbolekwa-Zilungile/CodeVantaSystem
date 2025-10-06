using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class EditPurchaseRequestViewModel
    {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpectedDate { get; set; }

        public RequestStatus Status { get; set; }
        public PriorityLevel Priority { get; set; }
        public string? Notes { get; set; }

        public List<PurchaseRequestItemViewModel> Items { get; set; } = new();
    }
}
