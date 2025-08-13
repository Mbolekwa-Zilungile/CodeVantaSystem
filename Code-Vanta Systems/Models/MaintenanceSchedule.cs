using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class MaintenanceSchedule
    {
        public int Id { get; set; }

        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        public DateTime NextServiceDate { get; set; }

        [StringLength(200)]
        public string Notes { get; set; }
    }
}
