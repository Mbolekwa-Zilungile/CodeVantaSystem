using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CreateEmployeeViewModel
    {
        // Identity Fields
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        // Employee/Profile Fields
        [Required]
        public string Name { get; set; }

        // Role Assignment Field
        [Required]
        public string SelectedRole { get; set; }

        // For Dropdown list population
        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}
