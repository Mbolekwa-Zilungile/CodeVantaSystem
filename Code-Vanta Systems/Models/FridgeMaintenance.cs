using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class FridgeMaintenance
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FridgeId { get; set; }
    public virtual Fridge Fridge { get; set; }

    [Required]
    public string TechnicianId { get; set; }
    public virtual ApplicationUser Technician { get; set; }

    [Required]
    public DateTime MaintenanceDate { get; set; }

    [Required, StringLength(200)]
    public string MaintenanceDescription { get; set; }

    [Required]
    public MaintenanceStatus Status { get; set; }
}
