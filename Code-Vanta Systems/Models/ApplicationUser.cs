namespace Code_Vanta_Systems.Models
{
    using Microsoft.AspNetCore.Identity;
    using System.ComponentModel.DataAnnotations;

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        // Navigation
        public virtual ICollection<FridgeFault> AssignedFaults { get; set; }
        public virtual ICollection<FridgeMaintenance> AssignedMaintenances {  get; set; }
        public virtual ICollection<PurchaseRequest> PurchaseRequest { get; set; }
    }
}
