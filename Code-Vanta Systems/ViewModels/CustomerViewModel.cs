using Microsoft.AspNetCore.Mvc.Rendering;
using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class CustomerViewModel
    {
        [Required]
        public string BusinessName { get; set; }

        [Required]
        public string ContactName { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string BusinessType { get; set; } // Spaza Shop, Shebeen, etc.

        [Required]
        public Region SelectedRegion { get; set; }

        public string PhysicalAddress { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public List<SelectListItem> RegionOptions { get; set; }
    }

}
