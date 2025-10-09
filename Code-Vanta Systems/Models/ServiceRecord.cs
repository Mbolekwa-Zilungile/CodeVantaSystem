using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class ServiceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }

        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        [Required]
        public DateTime DateOfService { get; set; }

        // User who performed the service
        [Required]
        public string TechnicianUserId { get; set; }

        [ForeignKey("TechnicianUserId")]
        public ApplicationUser Technician { get; set; }

        [Required]
        [StringLength(500)]
        public string ServiceDetails { get; set; }

        // Optional: Reference to a fault that was resolved
        public int? FaultId { get; set; }
        public FridgeFault Fault { get; set; }
    }
}