using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class UpdateCustomerViewModel
    {
        // Customer.Id (for the POST back)
        public int Id { get; set; }

        // ApplicationUser.UserId (for role/user updates)
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Business name is required")]
        public string BusinessName { get; set; }

        [Required(ErrorMessage = "Contact name is required")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Enter a valid phone number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } // Matches ApplicationUser.Email

        [Required(ErrorMessage = "Business Type is required")]
        public string BusinessType { get; set; }

        public string? PhysicalAddress { get; set; }

        // Location fields
        [Required(ErrorMessage = "Location is required")]
        public int LocationId { get; set; }
        public string? CurrentLocationDescription { get; set; }
        public List<SelectListItem> AvailableLocations { get; set; } = new();
    }
}
