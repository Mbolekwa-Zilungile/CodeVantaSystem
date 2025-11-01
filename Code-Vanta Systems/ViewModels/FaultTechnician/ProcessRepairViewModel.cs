using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FaultTechnician
{
    public class ProcessRepairViewModel
    {
        public int FaultId { get; set; }

        [Required(ErrorMessage = "Repair description is required")]
        [StringLength(500, ErrorMessage = "Repair description cannot exceed 500 characters")]
        public string RepairDescription { get; set; }

        [StringLength(200, ErrorMessage = "Parts used cannot exceed 200 characters")]
        public string PartsUsed { get; set; }

        [Required(ErrorMessage = "New fridge condition is required")]
        public FridgeCondition NewFridgeCondition { get; set; }

        // Additional properties for the updated data model
        public string FridgeModelName { get; set; }
        public string SerialNumber { get; set; }
        public string ReportedBy { get; set; }
        public DateTime DateReported { get; set; }
    }
}