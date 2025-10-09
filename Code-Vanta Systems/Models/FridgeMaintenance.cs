using Code_Vanta_Systems.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class FridgeMaintenance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }

        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        [Required]
        public string TechnicianId { get; set; }

        [ForeignKey("TechnicianId")]
        public ApplicationUser Technician { get; set; }

        // FIX for 'DatePerformed' error
        public DateTime? ActualCompletionDate { get; set; }

        // FIX for 'Description' error
        [MaxLength(1000)]
        public string MaintenanceDescription { get; set; }

        [MaxLength(500)]
        public string PartsUsed { get; set; }

        public MaintenanceStatus Status { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}