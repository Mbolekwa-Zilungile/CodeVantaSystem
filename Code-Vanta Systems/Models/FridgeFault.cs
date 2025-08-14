namespace Code_Vanta_Systems.Models
{
    using Code_Vanta_Systems.Models.Enums;
    using System.ComponentModel.DataAnnotations;

    public class FridgeFault
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        [Required]
        public string ReportedByUserId { get; set; }
        public virtual ApplicationUser ReportedBy { get; set; }

        public string AssignedTechnicianId { get; set; }
        public virtual ApplicationUser AssignedTechnician { get; set; }

        [Required]
        public DateTime DateReported { get; set; }

        [Required]
        [StringLength(500)]
        public string FaultDescription { get; set; }

        [Required]
        public FaultStatus Status { get; set; }

        public DateTime? ResolutionDate { get; set; }
    }
}
