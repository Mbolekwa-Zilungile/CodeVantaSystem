namespace Code_Vanta_Systems.Models
{
    using Microsoft.AspNetCore.Identity;
    using System.ComponentModel.DataAnnotations;

    public class ApplicationUser : IdentityUser
    {
        // Full name of the user (e.g., technician, requester, admin)
        [StringLength(100)]
        public string FullName { get; set; }

        // Navigation property for faults assigned to this user (as a technician)
        public virtual ICollection<FridgeFault> AssignedFaults { get; set; }

        // Navigation property for maintenance tasks assigned to this user
        public virtual ICollection<FridgeMaintenance> AssignedMaintenances {  get; set; }

        // Navigation property for purchase requests created by this user
        public virtual ICollection<PurchaseRequest> PurchaseRequest { get; set; }

        public virtual Employee EmployeeProfile { get; set; }

    }
}
