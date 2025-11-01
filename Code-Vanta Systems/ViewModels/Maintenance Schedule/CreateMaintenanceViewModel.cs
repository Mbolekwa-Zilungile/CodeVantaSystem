using System;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Maintenance_Schedule
{
    public class CreateMaintenanceViewModel
    {
        [Display(Name = "Fridge Unit")]
        [Required(ErrorMessage = "Please select a fridge unit.")]
        public int FridgeUnitId { get; set; } // Change from FridgeId to FridgeUnitId

        [Display(Name = "Maintenance Date")]
        [Required(ErrorMessage = "Please select a maintenance date.")]
        [DataType(DataType.Date)]
        public DateTime MaintenanceDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please provide a maintenance description.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string MaintenanceDescription { get; set; }

        [Display(Name = "Status")]
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

        // Optional: Add validation for future dates
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MaintenanceDate < DateTime.Now.Date)
            {
                yield return new ValidationResult(
                    "Maintenance date cannot be in the past.",
                    new[] { nameof(MaintenanceDate) });
            }
        }
    }
}