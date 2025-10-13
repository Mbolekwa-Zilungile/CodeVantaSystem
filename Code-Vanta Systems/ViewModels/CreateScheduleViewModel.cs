using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateScheduleViewModel
    {
        // Must be nullable (int?) to match the GET action parameter, 
        // allowing the form to load before a Fridge is selected.
        // It must be validated as required in the form.
        [Display(Name = "Fridge")]
        [Required(ErrorMessage = "Please select a fridge.")]
        public int? FridgeId { get; set; }

        // The TechnicianId is set in the controller, not by the view.

        [Required(ErrorMessage = "Scheduled Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Scheduled Date")]
        public DateTime ScheduledDate { get; set; }

        [Required(ErrorMessage = "Description of maintenance is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // Property to hold the list of available fridges for the dropdown/select list in the view
        public IEnumerable<SelectListItem> AvailableFridges { get; set; } = new List<SelectListItem>();
    }
}