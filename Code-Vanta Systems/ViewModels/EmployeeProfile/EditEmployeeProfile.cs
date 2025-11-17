using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.EmployeeProfile
{
    public class EditEmployeeProfile
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
