using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class ItemCatalog
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }  // Item name, e.g., "Fridge Compressor"

        [StringLength(500)]
        public string? Description { get; set; }  // Optional details about the item

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }  // Price per unit

        public bool IsActive { get; set; } = true;  // Only active items can be requested
    }
}