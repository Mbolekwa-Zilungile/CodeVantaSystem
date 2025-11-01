// CreateFaultReportViewModel.cs
using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.MaintenanceTechnician
{
    public class CreateFaultReportViewModel
    {
        [Required(ErrorMessage = "Please select the fridge reporting the fault.")]
        public int? FridgeId { get; set; }

        public int? FridgeUnitId { get; set; } // Added missing property

        public string? SerialNumber { get; set; }

        [Required(ErrorMessage = "Fault description is required.")]
        [Display(Name = "Fault Description")]
        [MaxLength(1000)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Severity is required.")]
        public FaultSeverity Severity { get; set; }

        // FIX: Must be present. Used by the Controller to populate the severity dropdown list.
        public IEnumerable<SelectListItem> AvailableFridges { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> AvailableFridgeUnits { get; set; } = new List<SelectListItem>(); // Added missing property
    }
}