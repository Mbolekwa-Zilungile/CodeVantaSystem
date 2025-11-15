using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using Code_Vanta_Systems.Models.Enums; // Assuming Enums are needed for FridgeCondition

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateServiceRecordViewModel
    {
        // FIX: Replaced FridgeId with FridgeUnitId
        [Required]
        public int? FridgeUnitId { get; set; }

        // CRITICAL: FridgeId is still needed if ServiceRecord model uses it (as per controller logic)
        public int? FridgeId { get; set; }

        // Make nullable or remove [Required] to prevent "SerialNumber is required" errors
        public string? SerialNumber { get; set; } // For display only

        [Required(ErrorMessage = "Date of service is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Service")]
        public DateTime DateOfService { get; set; } = DateTime.Now.Date;

        [Required(ErrorMessage = "Service details are required.")]
        [StringLength(500, ErrorMessage = "Service details cannot exceed 500 characters.")]
        [Display(Name = "Service Details/Actions Taken")]
        public string Description { get; set; } // FIX: Renamed ServiceDetails to Description to match controller usage

        [Display(Name = "Resolved Fault (Optional)")]
        public int? FaultId { get; set; }

        // NEW PROPERTIES added based on controller POST logic
        [Display(Name = "Parts Used")]
        [StringLength(500)]
        public string? PartsUsed { get; set; }

        [Required(ErrorMessage = "New condition after service is required.")]
        [Display(Name = "New Condition")]
        public FridgeCondition NewCondition { get; set; } // Assuming FridgeCondition enum exists

        [Display(Name = "Maintenance Type")]
        public int? MaintenanceTypeId { get; set; } // Assuming an int ID for Maintenance Type

        [Display(Name = "Total Cost")]
        [Range(0, double.MaxValue, ErrorMessage = "Cost must be positive.")]
        public decimal Cost { get; set; }

        // This is necessary to hold the list of faults fetched by the helper
        public IEnumerable<SelectListItem> AvailableFaults { get; set; } = new List<SelectListItem>();
        // Add a list for Maintenance Types if applicable
        public IEnumerable<SelectListItem>? AvailableMaintenanceTypes { get; set; }
    }
}