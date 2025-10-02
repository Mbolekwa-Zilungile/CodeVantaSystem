using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class EditSupplierViewModel
    {
        public int SupplierId { get; set; } // Supplier Id

        public string ApplicationUserId { get; set; }

        // User info
        [Required(ErrorMessage = "Contact Person is required")]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }


        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number")]
        [RegularExpression(@"^(\+27\d{9}|0\d{9})$", ErrorMessage = "Phone number must start with +27 or 0 and be 10 digits total")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        // Supplier info
        [Required(ErrorMessage = "Company name is required")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(300)]
        public string Address { get; set; }
       
    }
}