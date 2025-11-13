using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class FridgeFault
    {
        [Key]
        public int Id { get; set; }

        // OLD: FridgeId -> NEW: FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }
        public virtual FridgeUnit FridgeUnit { get; set; }

        [Required]
        public string ReportedByUserId { get; set; }
        public virtual ApplicationUser ReportedBy { get; set; }

        public string? AssignedTechnicianId { get; set; }
        public virtual ApplicationUser AssignedTechnician { get; set; }

        [Required]
        public DateTime DateReported { get; set; }

        [Required, StringLength(500)]
        public string FaultDescription { get; set; }

        public FaultSeverity Severity { get; set; }

        [StringLength(1000)]
        public string? RepairDescription { get; set; }

        [StringLength(500)]
        public string? PartsUsed { get; set; }

        [Required]
        public FaultStatus Status { get; set; }

        public DateTime? ResolutionDate { get; set; }
    }
}