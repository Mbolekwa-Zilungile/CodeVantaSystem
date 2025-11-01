using System;
using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Maintenance_Schedule
{
    public class MaintenanceViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Fridge Unit ID")]
        public int FridgeUnitId { get; set; } // Add this property

        [Display(Name = "Fridge Model")]
        public string FridgeModel { get; set; }

        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } // Add this property

        [Display(Name = "Customer ID")]
        public int CustomerId { get; set; }

        [Display(Name = "Business Name")]
        public string BusinessName { get; set; }

        [Display(Name = "Technician ID")]
        public string TechnicianId { get; set; }

        [Display(Name = "Technician Name")]
        public string TechnicianName { get; set; }

        [Display(Name = "Maintenance Date")]
        [DataType(DataType.Date)]
        public DateTime MaintenanceDate { get; set; }

        [Display(Name = "Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? ActualCompletionDate { get; set; }

        [Display(Name = "Description")]
        public string MaintenanceDescription { get; set; }

        [Display(Name = "Status")]
        public MaintenanceStatus Status { get; set; }

        // Additional properties for better display
        [Display(Name = "Days Until Maintenance")]
        public int DaysUntilMaintenance => (MaintenanceDate - DateTime.Now).Days;

        [Display(Name = "Is Overdue")]
        public bool IsOverdue => Status == MaintenanceStatus.Overdue ||
                                (Status == MaintenanceStatus.Scheduled && MaintenanceDate < DateTime.Now);

        [Display(Name = "Is Completed")]
        public bool IsCompleted => Status == MaintenanceStatus.Completed;
    }
}