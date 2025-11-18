using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeUnit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }

        [ForeignKey(nameof(FridgeId))]
        public virtual Fridge Fridge { get; set; }

        [Required, StringLength(100)]
        public string SerialNumber { get; set; }

        [Required]
        public FridgeCondition Condition { get; set; }

        [Required]
        public FridgeStatus Status { get; set; }

        // Assigned Customer (nullable until fridge is allocated)
        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        // Assigned Technician (nullable until fridge is under maintenance)

      
        public string? TechnicianUserId { get; set; }

        [ForeignKey(nameof(TechnicianUserId))]
        public virtual ApplicationUser? Technician { get; set; }

        public bool IsRented { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Maintenance Schedules linked directly to this unit
        public virtual ICollection<MaintenanceSchedule>? MaintenanceSchedules { get; set; }

        // Collection of faults associated with this specific unit
        public virtual ICollection<FridgeFault>? Faults { get; set; }

        // Collection of service records associated with this specific unit
        public virtual ICollection<ServiceRecord>? ServiceRecords { get; set; }
    }
}
