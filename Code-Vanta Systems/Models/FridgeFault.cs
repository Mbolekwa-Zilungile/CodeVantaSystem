using Code_Vanta_Systems.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class FridgeFault
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }

        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        [Required]
        public DateTime DateReported { get; set; }

        [Required]
        [MaxLength(1000)]
        public string FaultDescription { get; set; }

        [Required]
        public string ReportedByUserId { get; set; }

        [ForeignKey("ReportedByUserId")]
        public ApplicationUser ReportedBy { get; set; }

        // FIX: Add the required foreign key for the assigned technician
        public string? AssignedTechnicianId { get; set; }

        // FIX: Add the missing navigation property that the Configuration file is looking for
        [ForeignKey("AssignedTechnicianId")]
        public ApplicationUser? AssignedTechnician { get; set; }

        public FaultStatus Status { get; set; }
        public FaultSeverity Severity { get; set; }
    }
}