namespace Code_Vanta_Systems.Models
{
    using Code_Vanta_Systems.Models.Enums;
    public class FridgeFault
    {
        public int Id { get; set; }

        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        public string ReportedByUserId { get; set; }
        public virtual ApplicationUser ReportedBy { get; set; }

        public string AssignedTechnicianId { get; set; }
        public virtual ApplicationUser AssignedTechnician { get; set; }

        public DateTime DateReported { get; set; }
        public string FaultDescription { get; set; }

        public FaultStatus Status { get; set; }
        public DateTime? ResolutionDate { get; set; }
    }
}
