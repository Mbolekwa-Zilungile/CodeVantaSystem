using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class ServiceRecord
    {
        [Key]
        public int Id { get; set; }

        // CHANGE: FridgeId → FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }

        [ForeignKey("FridgeUnitId")]
        public FridgeUnit FridgeUnit { get; set; }

        [Required]
        public DateTime DateOfService { get; set; }

        [Required]
        public string TechnicianUserId { get; set; }

        [ForeignKey("TechnicianUserId")]
        public ApplicationUser Technician { get; set; }

        [Required]
        [StringLength(500)]
        public string ServiceDetails { get; set; }

        public int? FaultId { get; set; }
        public FridgeFault Fault { get; set; }

        [StringLength(500)]
        public string? PartsUsed { get; set; }
    }
}