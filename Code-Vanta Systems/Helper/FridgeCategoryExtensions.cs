using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Helper
{
    public static class FridgeCategoryExtensions
    {
        public static List<string> GetModels(this FridgeCategory category)
        {
            return category switch
            {
                FridgeCategory.ChestFreezers => new List<string> { "CF-100", "CF-200" },
                FridgeCategory.ColdRooms => new List<string> { "CR-Small", "CR-Large" },
                FridgeCategory.WineBabinets => new List<string> { "WB-Mini", "WB-Pro" },
                FridgeCategory.BackBarCoolers => new List<string> { "BB-Standard", "BB-Deluxe" },
                FridgeCategory.Multidecks => new List<string> { "MD-Slim", "MD-Wide" },
                _ => new List<string>()
            };
        }
    }
}
