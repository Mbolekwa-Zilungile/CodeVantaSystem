namespace Code_Vanta_Systems.Models
{
    using Code_Vanta_Systems.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class FridgeFault
    {
        [Key]
        public int Id { get; set; }

        // Foreign key referencing the fridge with the fault
        [Required]
        public int FridgeId { get; set; }

        // Navigation property for the fridge
        public virtual Fridge Fridge { get; set; }

        // Foreign key referencing the user who reported the fault
        [Required]
        public string ReportedByUserId { get; set; }

        // Navigation property for the reporting user
        public virtual ApplicationUser ReportedBy { get; set; }

        // Foreign key referencing the technician assigned to resolve the fault
        public string AssignedTechnicianId { get; set; }

        // Navigation property for the assigned technician
        public virtual ApplicationUser AssignedTechnician { get; set; }

        // Date when the fault was reported
        [Required]
        public DateTime DateReported { get; set; }

        // Description of the fault
        [Required]
        [StringLength(500)]
        public string FaultDescription { get; set; }

        // Current status of the fault (e.g Open, InProgress, Resolved)
        [Required]
        public FaultStatus Status { get; set; }

        // Date when the fault was resolved (nullable)
        public DateTime? ResolutionDate { get; set; }
    }
}
