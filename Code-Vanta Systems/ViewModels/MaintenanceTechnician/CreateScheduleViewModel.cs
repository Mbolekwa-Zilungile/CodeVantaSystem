// CreateScheduleViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Code_Vanta_Systems.ViewModels.MaintenanceTechnician
{
    public class CreateScheduleViewModel
    {
        // Context/Hidden Fields
        [Display(Name = "Fridge")]
        [Required(ErrorMessage = "Please select a fridge.")]
        public int? FridgeId { get; set; }

        public int? FridgeUnitId { get; set; } // Added missing property

        // For display in the View's title/heading (e.g., "ZBD300 (N/A Customer)")
        public string? FridgeDisplay { get; set; }

        [Required(ErrorMessage = "Scheduled Date and Time are required.")]
        // Using DateTime to capture the time slot for the visit
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Date and Time")]
        public DateTime ScheduledDate { get; set; } = DateTime.Now.Date.AddHours(9).AddDays(7); // Default to 9:00 AM next week

        [Required(ErrorMessage = "Description of maintenance is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // Property to hold the list of available fridges for the dropdown/select list 
        public IEnumerable<SelectListItem> AvailableFridges { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> AvailableFridgeUnits { get; set; } = new List<SelectListItem>(); // Added missing property
    }
}