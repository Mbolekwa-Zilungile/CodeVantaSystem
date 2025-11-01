using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models.Enums
{
    public enum FridgeCategory
    {
        [Display(Name = "Chest Freezers")]
        ChestFreezers,

        [Display(Name = "Cold Rooms")]
        ColdRooms,

        [Display(Name = "Wine Cabinets")]
        WineBabinets,

        [Display(Name = "Back Bar Coolers")]
        BackBarCoolers,

        [Display(Name = "Multidecks")]
        Multidecks,

        [Display(Name = "Upright Freezers")]
        UprightFreezers,

        [Display(Name = "Display Refrigerators")]
        DisplayRefrigerators,

        [Display(Name = "Undercounter Refrigerators")]
        UndercounterRefrigerators,
    }
}
