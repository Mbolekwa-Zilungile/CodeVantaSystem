// File: ViewModels/RegisterCustomerViewModel.cs

using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RegisterCustomerViewModel
    {
        // ------------------ Identity User Fields ------------------
        // Note: FullName is used as ContactName in the Customer entity for simplicity

        [Required(ErrorMessage = "Contact name is required")]
        [Display(Name = "Contact Person Full Name")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Email address is required (for Login)")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number")]
        public string Phone { get; set; }

        // ------------------ Customer Entity Fields ------------------

        [Required(ErrorMessage = "Business Name is required")]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Business Type is required")]
        [Display(Name = "Business Type (e.g., Spaza Shop)")]
        public string BusinessType { get; set; }

        [Required(ErrorMessage = "Physical Address is required")]
        [Display(Name = "Physical Address")]
        [StringLength(250)]
        public string PhysicalAddress { get; set; }

        // ------------------ Location Dropdown ------------------


        [Required(ErrorMessage = "Please select a location/region.")]
        public int SelectedLocationId { get; set; }

        // List of available locations for the dropdown
        public List<SelectListItem> AvailableLocations { get; set; } = new();
    }
}