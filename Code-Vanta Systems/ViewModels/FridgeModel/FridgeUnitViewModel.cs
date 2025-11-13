using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.InventoryLiaison
{
    public class FridgeUnitViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Model")]
        public string ModelName { get; set; }

        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        public FridgeStatus Status { get; set; }

        public FridgeCondition Condition { get; set; }

        [Display(Name = "Current Location")]
        public string LocationName { get; set; }
    }
}