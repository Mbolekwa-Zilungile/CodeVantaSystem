using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class FridgeMaintenance
    {
        [Key]
        public int Id { get; set; }

        //Foreign key referencing the fridge being maintained
        public int FridgeId { get; set; }
        //Navigation property for the fridge being maintained
        public virtual Fridge Fridge { get; set; }

        public string TechnicianId { get; set; }
        public virtual ApplicationUser Technician { get; set; }

        //The date the maintenance was performed
        public DateTime MaintenanceDate { get; set; }

        //Description of the maintenance work performed
        [Required, StringLength(200)]
        public string MaintenanceDescription { get; set; }

        // Current status of the maintenance (e.g Scheduled, Completed)
        public MaintenanceStatus Status { get; set; }
    }
}
