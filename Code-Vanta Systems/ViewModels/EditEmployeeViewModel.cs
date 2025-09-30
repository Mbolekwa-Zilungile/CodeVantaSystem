using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{ 
        // For editing existing employee
        public class EditEmployeeViewModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Full name is required")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Email address is required")]
            [EmailAddress(ErrorMessage = "Enter a valid email address")]
            public string Email { get; set; }

            [Phone(ErrorMessage = "Enter a valid phone number")]
            [RegularExpression(@"^(\+27\d{9}|0\d{9})$", ErrorMessage = "Phone number must start with +27 or 0 and be 10 digits total")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Please select a role.")]
            public string SelectedRole { get; set; }

            public List<SelectListItem> Roles { get; set; } = new();
        }
}
