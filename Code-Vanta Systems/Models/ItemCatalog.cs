using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Code_Vanta_Systems.Models.Enums; // Ensure you have this namespace for enums

namespace Code_Vanta_Systems.Models
{
    public class ItemCatalog
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // The property that caused the warning (fixed in config below)
        [Required]
        public decimal UnitPrice { get; set; }

        // The property that caused the error - Category
        [Required]
        public ItemCategory Category { get; set; }

        // ADDED THE MISSING PROPERTY
        public bool IsActive { get; set; } = true;

        // Foreign Key to Supplier - FIX for Supplier error
        [Required]
        public int SupplierId { get; set; }

        // Navigation Property - FIX for Supplier error
        public virtual Supplier Supplier { get; set; }
    }
}