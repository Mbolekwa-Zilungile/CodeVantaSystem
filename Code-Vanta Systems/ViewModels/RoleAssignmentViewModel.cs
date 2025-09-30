using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RoleAssignmentViewModel
    {
        // Hidden field to identify the user being edited
        public string UserId { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }

        // List of all available roles for the dropdown
        public List<SelectListItem> AvailableRoles { get; set; } = new();

        // The single role the Admin selects from the dropdown
        [Required(ErrorMessage = "A role must be selected.")]
        public string SelectedRole { get; set; }
    }
}