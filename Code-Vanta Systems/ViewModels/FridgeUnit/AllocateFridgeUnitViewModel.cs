using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.FridgeUnit
{
    public class AllocateFridgeUnitViewModel
    {
        public int FridgeUnitId { get; set; }
        public string SerialNumber { get; set; }
        public string FridgeModelName { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }
        public List<SelectListItem> Customers { get; set; }
    }
}
