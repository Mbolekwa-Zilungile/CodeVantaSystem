using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Helpers; // for EnumExtensions
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Code_Vanta_Systems.Services
{
    public interface IDropdownService
    {
        List<SelectListItem> GetRegionOptions();
    }

    public class DropdownService : IDropdownService
    {
        public List<SelectListItem> GetRegionOptions()
        {
            return Enum.GetValues(typeof(Region))
                       .Cast<Region>()
                       .Select(r => new SelectListItem
                       {
                           Value = ((int)r).ToString(),
                           Text = r.GetDisplayName() // uses your EnumExtensions
                       })
                       .ToList();
        }
    }
}
