using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class EditSupplierViewModel
    {
        public int Id { get; set; } // Supplier Id

        public string ApplicationUserId { get; set; }

        // User info
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        // Supplier info
        [Required]
        [Display(Name = "Company Name")]
        public string Name { get; set; }

        [Required]
        [StringLength(300)]
        public string Address { get; set; }

        // Account status
        [Display(Name = "Is Active?")]
        public bool IsActive { get; set; } = true;
    }
}