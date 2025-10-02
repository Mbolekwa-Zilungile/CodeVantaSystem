using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class RegisterSupplierViewModel
    {
        [Required(ErrorMessage = "Contact person's name is required")]
        public string FullName { get; set; }


        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number")]
        [RegularExpression(@"^(\+27\d{9}|0\d{9})$", ErrorMessage = "Phone number must start with +27 or 0 and be 10 digits total")]
        public string Phone { get; set; }

        // From Supplier
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100)]
        public string Name { get; set; } // Company name

        [Required]
        [StringLength(300)]
        public string Address { get; set; }
    }
}
