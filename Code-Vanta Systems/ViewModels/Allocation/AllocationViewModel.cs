using System.ComponentModel.DataAnnotations;

public class AllocationViewModel
{
    [Required(ErrorMessage = "Customer is required")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Fridge Model is required")]
    [Display(Name = "Fridge Model")]
    public int FridgeModelId { get; set; } // ✅ FridgeModelId, not FridgeId

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; } = 1;
}