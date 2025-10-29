// ProcessRepairViewModel.cs
using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FaultTechnician
{
    public class ProcessRepairViewModel
    {
        [Required]
        public int FaultId { get; set; }

        [Required(ErrorMessage = "A repair description is required to close the fault.")]
        [StringLength(1000)]
        public string RepairDescription { get; set; }

        [StringLength(500)]
        public string? PartsUsed { get; set; }

        [Required(ErrorMessage = "The final fridge condition must be selected.")]
        // FridgeCondition is assumed to be an existing enum (e.g., Good, Scrapped, ReturnedToStock)
        public FridgeCondition NewFridgeCondition { get; set; }
    }
}