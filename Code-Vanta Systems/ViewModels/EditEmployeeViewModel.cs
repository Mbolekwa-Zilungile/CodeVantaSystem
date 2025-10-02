using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Code_Vanta_Systems.ViewModels
{
    // For editing an existing employee profile
    public class EditEmployeeViewModel
    {
        // 1. Identifiers (MUST match the controller's property names)
        [Required]
        public int EmployeeId { get; set; } // CRITICAL FIX: Matches controller's lookup key

        [NotNull]
        public string ApplicationUserId { get; set; } = string.Empty; // CRITICAL FIX: Matches controller's user lookup

        // 2. ApplicationUser Details
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Login Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Enter a valid phone number")]
        [RegularExpression(@"^(\+27\d{9}|0\d{9})$", ErrorMessage = "Phone number must start with +27 or 0 and be 10 digits total")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // 3. Employee Entity Details (Work specific)
        [Required(ErrorMessage = "Please select a role.")]
        [Display(Name = "System Role")]
        public string SelectedRole { get; set; } = string.Empty; // CRITICAL FIX: Matches controller's role property

        [Display(Name = "Position/Job Title")] // New field for job title
        public string? Position { get; set; }

        [Display(Name = "Employee is Active")]
        public bool IsActive { get; set; } = true;

        // 4. Location Details
        [Display(Name = "Assigned Depot/Location (Optional)")]
        public int? LocationId { get; set; } // Nullable int matches the Employee model

        // 5. Dropdown Data (MUST match the controller's property names)
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>(); // CRITICAL FIX: Matches controller's list property

        public List<SelectListItem> AvailableLocations { get; set; } = new List<SelectListItem>(); // Location dropdown list
    }
}
