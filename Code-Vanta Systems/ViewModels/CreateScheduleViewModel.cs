using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateScheduleViewModel
    {
        // FIX: Replaced FridgeId with FridgeUnitId
        [Required(ErrorMessage = "A fridge unit must be selected.")]
        [Display(Name = "Fridge Unit")]
        public int? FridgeUnitId { get; set; }

        [Required(ErrorMessage = "Scheduled date is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime ScheduledDate { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(500)]
        public string Description { get; set; }

        // Used to store the selected fridge unit's display name for success messages
        public string? FridgeDisplay { get; set; }

        // Used by the Controller to populate the unit dropdown list
        public IEnumerable<SelectListItem> AvailableFridges { get; set; } = new List<SelectListItem>();
    }
}