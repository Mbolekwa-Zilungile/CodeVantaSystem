using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RegisterPurchaseRequestViewModel
    {
        
            [Required]
            public PriorityLevel Priority { get; set; }

            [DataType(DataType.Date)]
            public DateTime? ExpectedDate { get; set; }

            [StringLength(1000)]
        public string? Notes { get; set; }

    }
}
