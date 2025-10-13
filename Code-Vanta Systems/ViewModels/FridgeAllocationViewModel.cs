using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectListItem

namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeAllocationViewModel
    {
        // 1. Core Allocation Properties
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer selection is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Fridge selection is required.")]
        [Display(Name = "Fridge Serial Number/Model")]
        public int FridgeId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        [Display(Name = "Quantity Allocated")]
        public int Quantity { get; set; }

        // 2. CRITICAL FIX: Technician Assignment Property
        [Required(ErrorMessage = "A maintenance technician must be assigned.")]
        [Display(Name = "Assigned Maintenance Technician")]
        public string AssignedTechnicianId { get; set; }


        // 3. Dropdown List Collections (for the GET action to populate the View)
        public List<SelectListItem> AvailableCustomers { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> AvailableFridges { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> AvailableTechnicians { get; set; } = new List<SelectListItem>();
    }
}