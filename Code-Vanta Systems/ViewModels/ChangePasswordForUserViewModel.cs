// File: ViewModels/ChangePasswordForUserViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class ChangePasswordForUserViewModel
    {
        // Holds the ApplicationUser ID passed from the URL
        [Required]
        public string UserId { get; set; }

        // Display field for context (fetched from the database)
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "New Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}