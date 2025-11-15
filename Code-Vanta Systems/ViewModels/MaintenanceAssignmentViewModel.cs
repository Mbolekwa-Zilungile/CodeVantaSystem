// MaintenanceAssignmentViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class MaintenanceAssignmentViewModel
    {
        // Fridge Identifier
        [Display(Name = "Fridge ID")]
        public int FridgeId { get; set; }

        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; }

        public string Model { get; set; }

        public string Condition { get; set; } // Current fridge condition (should be 'RequiresMaintenance')

        // Customer Information
        [Display(Name = "Customer Name")]
        public string CustomerBusinessName { get; set; }

        [Display(Name = "Location Address")]
        public string CustomerAddress { get; set; } // Crucial for location-based assignment

        // Current Assignment Information (for display purposes)
        [Display(Name = "Current Technician")]
        public string? AllocatedTechnicianName { get; set; } // Name of the technician currently managing the customer/fridge

        public string? AllocatedTechnicianId { get; set; } // ID of the technician currently managing the customer/fridge

        // Renamed for clarity to match the underlying data structure in Fridge.cs
        [Display(Name = "Current Technician")]

        public string? TechnicianName { get; set; }

        // New Assignment Target (used for the form POST)
        // Note: The actual form post uses simple types (int fridgeId, string technicianId),
        // but this property can be useful if you were to use this VM for the POST as well.
        public string? TechnicianId { get; set; }

        // --- NEW FIELDS FOR ADVANTAGEOUS OVERSIGHT ---
        public int DaysAssigned { get; set; } // Time waiting/in progress
        public int FaultCountYTD { get; set; } // High-cost unit identification
        public int ServiceCountYTD { get; set; } // High-cost unit identification
    }
}