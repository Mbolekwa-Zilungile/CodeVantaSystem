using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Code_Vanta_Systems.Models.Enums;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 40 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Business name is required.")]
    public string BusinessName { get; set; }

    [Required(ErrorMessage = "Business type is required.")]
    [Display(Name = "Business Type")]
    public string BusinessType { get; set; } // Spaza Shop, Shebeen, Other

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Region is required.")]
    [Display(Name = "Region")]
    public Region SelectedRegion { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [Display(Name = "Location")]
    public int SelectedLocationId { get; set; }  // 🔹 Link to Location table

    [Display(Name = "Physical Address")]
    public string PhysicalAddress { get; set; }

    // Dropdowns
    [ValidateNever]
    public List<SelectListItem> RegionOptions { get; set; } = new();

    [ValidateNever]
    public List<SelectListItem> LocationOptions { get; set; } = new();
}
