using Code_Vanta_Systems.Models;
using System.ComponentModel.DataAnnotations;

public class FridgeAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FridgeId { get; set; }
    public virtual Fridge Fridge { get; set; }

    [Required]
    public int CustomerId { get; set; }
    public virtual Customer Customer { get; set; }

    [Required]
    public DateTime AllocationDate { get; set; }

    public DateTime? DeallocationDate { get; set; }
}
