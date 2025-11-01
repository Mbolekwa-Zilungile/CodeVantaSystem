using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class CreateFridgeUnitViewModel
{
    [Required]
    [StringLength(100)]
    public string SerialNumber { get; set; }

    [Required]
    [Display(Name = "Fridge Model")]
    public int FridgeModelId { get; set; }
    public List<SelectListItem> FridgeModels { get; set; }

    [Required]
    [Display(Name = "Condition")]
    public FridgeCondition Condition { get; set; }
    public List<SelectListItem> Conditions { get; set; }

    [Required]
    [Display(Name = "Location")]
    public int LocationId { get; set; }
    public List<SelectListItem> Locations { get; set; }

    [Required]
    [Display(Name = "Purchase Date")]
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
}