using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.InventoryLiaison
{
    public class CreateFridgeUnitViewModel
    {
        [Required(ErrorMessage = "Fridge Model is required")]
        [Display(Name = "Fridge Model")]
        public int FridgeModelId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Required]
        public FridgeCondition Condition { get; set; }

        [Required]
        public FridgeStatus Status { get; set; }

        [Required(ErrorMessage = "Initial Location is required")]
        [Display(Name = "Initial Location")]
        public int LocationId { get; set; }
    }
}