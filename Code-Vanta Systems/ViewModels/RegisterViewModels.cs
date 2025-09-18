using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(40, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Business name is required.")]
    public string BusinessName { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    public string Phone { get; set; }

    [Required(ErrorMessage = "Region is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid region.")]
    [Display(Name = "Region")]
    public Region SelectedRegion { get; set; }

    // ✅ Exclude from model validation
    [ValidateNever]
    public List<SelectListItem> RegionOptions { get; set; } = new();
}
