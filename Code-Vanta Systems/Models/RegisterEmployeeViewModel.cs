using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class RegisterEmployeeViewModel
    {
        public int Id { get; set; } 
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required,DataType(DataType.Password)]
        public string Password { set; get; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string SelectedRole { get; set; }

        [BindNever]
        [ValidateNever]
        public IEnumerable<SelectListItem> Roles { get; set; } 
        public string Role {  get; set; }
    }
}


