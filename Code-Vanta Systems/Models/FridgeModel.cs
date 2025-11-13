using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Code_Vanta_Systems.Models
{
    // Defines the TYPE or MODEL of fridge (e.g., 'Coca-Cola Slimline').
    // Replaces the inventory/catalogue properties of the old Fridge.cs.
    public class FridgeModel
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string ModelName { get; set; } // Renamed from Model to be explicit

        [Required]
        public FridgeCategory Category { get; set; } // Retained from Fridge.cs

        [StringLength(250)]
        public string? ImageUrl { get; set; } // Retained from Fridge.cs

        [Required(ErrorMessage = "Rental Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental Price must be positive")]
        [Precision(10, 2)]
        public decimal RentalPricePerMonth { get; set; } // Retained from Fridge.cs

        [StringLength(500)]
        public string? Description { get; set; } // Retained from Fridge.cs

        // Foreign Key: Supplier of this particular model type
        [Required]
        public int SupplierId { get; set; } // Retained from Fridge.cs
        public virtual Supplier Supplier { get; set; }

        // Navigation collection for all physical units of this model
        public virtual ICollection<FridgeUnit> Units { get; set; } = new HashSet<FridgeUnit>();
    }
}