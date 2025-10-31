using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.EmployeeProfile
{
    public class EmployeeProfileViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        // New password fields for editing
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }
    }
}
