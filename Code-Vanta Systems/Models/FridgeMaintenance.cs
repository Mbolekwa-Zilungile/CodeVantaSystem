using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class FridgeMaintenance
    {
        public int Id { get; set; }

        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        public string TechnicianId { get; set; }
        public virtual ApplicationUser Technician { get; set; }

        public DateTime MaintenanceDate { get; set; }

        [Required, StringLength(200)]
        public string MaintenanceDescription { get; set; }

        public MaintenanceStatus Status { get; set; }
    }
}
