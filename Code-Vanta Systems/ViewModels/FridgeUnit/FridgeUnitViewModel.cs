using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

using Code_Vanta_Systems.ViewModels.Shared;

namespace Code_Vanta_Systems.ViewModels.FridgeUnit
{
    public class FridgeUnitViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Display(Name = "FridgeModel")]
        public string FridgeModelName { get; set; }

        [Display(Name = "Category")]
        public FridgeCategory Category { get; set; }

        [Required]
        [Display(Name = "Status")]
        public FridgeStatus Status { get; set; }

        [Required]
        [Display(Name = "Condition")]
        public FridgeCondition Condition { get; set; }

        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "Location")]
        public int LocationId { get; set; }

        [Display(Name = "Monthly Rental Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental price must be positive")]
        public decimal RentalPricePerMonth { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public string ImageUrl { get; set; }

        // Navigation properties for dropdowns
        public List<SupplierDropdownViewModel> Suppliers { get; set; }
        public List<LocationDropdownViewModel> Locations { get; set; }
        public List<FridgeModelDropdownViewModel> FridgeModels { get; set; }
    }
}