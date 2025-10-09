using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels
{
    public class AllocationViewModel
    {
        [Required]
        public int FridgeId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
