using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Services
{
    public class DropdownService
    {
        public List<SelectListItem> GetRegionOptions()
        {
            var options = Enum.GetValues(typeof(Region))
                .Cast<Region>()
                .Select(r => new SelectListItem
                {
                    Value = ((int)r).ToString(), // integer value for binding
                    Text = GetEnumDisplayName(r) // user-friendly name
                })
                .ToList();

            // 🌟 FIX: Insert the default prompt option at the start of the list 🌟
            options.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Select your region --",
                Selected = true,
                Disabled = true
            });

            return options;
        }

        private string GetEnumDisplayName(Region region)
        {
            var memberInfo = typeof(Region).GetMember(region.ToString()).FirstOrDefault();
            var displayAttr = memberInfo?.GetCustomAttribute<DisplayAttribute>();
            return displayAttr?.Name ?? region.ToString();
        }
    }

}