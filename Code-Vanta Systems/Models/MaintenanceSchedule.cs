using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Azure.Core.HttpHeader;

namespace Code_Vanta_Systems.Models
{
    public class MaintenanceSchedule
    {
        [Key]
        public int Id { get; set; }
        // Foreign key referencing the fridge scheduled for maintenance
        [Required]
        public int FridgeId { get; set; }
        // Navigation property for the fridge
        public virtual Fridge Fridge { get; set; }

        // Date when the next maintenance is scheduled
        [Required]
        public DateTime NextServiceDate { get; set; }

        //notes about the maintenance schedule
        [StringLength(200)]
        public string Notes { get; set; }
    }
}
