using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models.Enums
{
    public enum FridgeCategory
    {
        [Display(Name = "Chest Freezers")]
        ChestFreezers,

        [Display(Name = "Cold Rooms")]
        ColdRooms,

        [Display(Name = "Wine Babinets")]
        WineBabinets,

        [Display(Name = "Back Bar Coolers")]
        BackBarCoolers,

        [Display(Name = "Multidecks")]
        Multidecks
    }
}
