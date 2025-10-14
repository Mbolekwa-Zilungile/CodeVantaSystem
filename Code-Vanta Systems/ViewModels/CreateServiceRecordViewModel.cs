using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateServiceRecordViewModel
    {
        [Required]
        public int FridgeId { get; set; }

        // Make nullable or remove [Required] to prevent "SerialNumber is required" errors
        public string? SerialNumber { get; set; } // For display only

        [Required(ErrorMessage = "Date of service is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Service")]
        public DateTime DateOfService { get; set; } = DateTime.Now.Date;

        [Required(ErrorMessage = "Service details are required.")]
        [StringLength(500, ErrorMessage = "Service details cannot exceed 500 characters.")]
        [Display(Name = "Service Details/Actions Taken")]
        public string ServiceDetails { get; set; }

        [Display(Name = "Resolved Fault (Optional)")]
        public int? FaultId { get; set; }

        // This is necessary to hold the list of faults fetched by the helper
        public IEnumerable<SelectListItem> AvailableFaults { get; set; } = new List<SelectListItem>();
    }
}