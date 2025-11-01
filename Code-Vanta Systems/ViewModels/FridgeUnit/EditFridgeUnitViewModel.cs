using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FridgeUnit
{
    public class EditFridgeUnitViewModel
    {
        
        public int Id { get; set; }

        [Required]
        public string SerialNumber { get; set; }

        [Required]
        public int FridgeModelId { get; set; }

        [Required]
        public FridgeStatus Status { get; set; }

        [Required]
        public FridgeCondition Condition { get; set; }

        [Required]
        public int LocationId { get; set; }

        public string TechnicianUserId { get; set; }
        public int? CurrentCustomerId { get; set; }

        public List<SelectListItem> FridgeModels { get; set; } = new();
        public List<SelectListItem> Locations { get; set; } = new();
        public List<SelectListItem> Technicians { get; set; } = new();
        public List<SelectListItem> Customers { get; set; } = new();
    }
}
