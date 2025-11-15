using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Added for NotMapped if necessary

namespace Code_Vanta_Systems.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }

        // One-to-One Navigation Properties
        public virtual Employee EmployeeProfile { get; set; }
        public virtual Customer CustomerProfile { get; set; } // <--- ADDED for one-to-one link

        // One-to-Many Navigation Collections
        public virtual ICollection<FridgeFault> AssignedFaults { get; set; } // Technician role
        public virtual ICollection<FridgeFault> ReportedFaults { get; set; } // User/Customer role
        public virtual ICollection<FridgeMaintenance> AssignedMaintenances { get; set; }
        // FIX: Add the missing MaintenanceSchedules collection
        public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; }

        // Purchasing & Scheduling Audit Trails
        public virtual ICollection<PurchaseRequest> PurchaseRequests { get; set; }
        public virtual ICollection<TechnicianSchedule> TechnicianSchedules { get; set; } // <--- ADDED for Schedule
        public virtual ICollection<PurchaseOrder> PlacedOrders { get; set; } // <--- ADDED for Purchase Orders
        public virtual ICollection<FridgeAllocation> CreatedAllocations { get; set; } // <--- ADDED for Allocations
        public virtual ICollection<FridgeRequest> SubmittedRequests { get; set; } // <--- ADDED for Fridge Requests

        public ApplicationUser()
        {
            AssignedFaults = new HashSet<FridgeFault>();
            ReportedFaults = new HashSet<FridgeFault>();
            AssignedMaintenances = new HashSet<FridgeMaintenance>();
            // FIX: Initialize the new collection
            MaintenanceSchedules = new HashSet<MaintenanceSchedule>();
            PurchaseRequests = new HashSet<PurchaseRequest>();
            TechnicianSchedules = new HashSet<TechnicianSchedule>();
            PlacedOrders = new HashSet<PurchaseOrder>();
            CreatedAllocations = new HashSet<FridgeAllocation>();
            SubmittedRequests = new HashSet<FridgeRequest>();
        }
    }
}