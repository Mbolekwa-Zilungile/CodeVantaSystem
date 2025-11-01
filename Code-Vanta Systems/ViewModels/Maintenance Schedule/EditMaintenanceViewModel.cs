using System;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Maintenance_Schedule
{
    public class EditMaintenanceViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Fridge Unit ID")]
        public int FridgeUnitId { get; set; }

        [Display(Name = "Maintenance Date")]
        [Required(ErrorMessage = "Please select a maintenance date.")]
        [DataType(DataType.Date)]
        public DateTime MaintenanceDate { get; set; }

        [Display(Name = "Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? ActualCompletionDate { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Please provide a maintenance description.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string MaintenanceDescription { get; set; }

        [Display(Name = "Status")]
        public MaintenanceStatus Status { get; set; }

        // Read-only properties for display
        [Display(Name = "Fridge Model")]
        public string FridgeModel { get; set; }

        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        [Display(Name = "Customer")]
        public string CustomerName { get; set; }

        // Validation for completion date
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Status == MaintenanceStatus.Completed && !ActualCompletionDate.HasValue)
            {
                yield return new ValidationResult(
                    "Completion date is required when status is set to Completed.",
                    new[] { nameof(ActualCompletionDate) });
            }

            if (ActualCompletionDate.HasValue && ActualCompletionDate.Value > DateTime.Now)
            {
                yield return new ValidationResult(
                    "Completion date cannot be in the future.",
                    new[] { nameof(ActualCompletionDate) });
            }
        }
    }
}