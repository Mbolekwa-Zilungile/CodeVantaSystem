using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }

        // Faults assigned to this user (technician role)
        public virtual ICollection<FridgeFault> AssignedFaults { get; set; }

        // Faults reported by this user (customer role)
        public virtual ICollection<FridgeFault> ReportedFaults { get; set; }

        // Maintenance tasks assigned to this user
        public virtual ICollection<FridgeMaintenance> AssignedMaintenances { get; set; }

        // Purchase requests created by this user
        public virtual ICollection<PurchaseRequest> PurchaseRequests { get; set; }

        // Employee profile (role, etc.)
        public virtual Employee EmployeeProfile { get; set; }

        public ApplicationUser()
        {
            AssignedFaults = new HashSet<FridgeFault>();
            ReportedFaults = new HashSet<FridgeFault>();
            AssignedMaintenances = new HashSet<FridgeMaintenance>();
            PurchaseRequests = new HashSet<PurchaseRequest>();
        }
    }
}
