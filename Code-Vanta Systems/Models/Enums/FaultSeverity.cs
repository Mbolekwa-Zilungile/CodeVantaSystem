using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models.Enums
{
    public enum FaultSeverity
    {
        [Display(Name = "Low")]
        Low,
        [Display(Name = "Medium")]
        Medium,
        [Display(Name = "High")]
        High,
        [Display(Name = "Critical")]
        Critical
    }
}