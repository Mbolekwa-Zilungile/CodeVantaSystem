using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Models
{
    public class FridgeModel
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } // "CoolMaster 5000L"

        [Required]
        public FridgeCategory Category { get; set; }

        [Required(ErrorMessage = "Rental Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Rental Price must be positive")]
        [Precision(10, 1)]
        public decimal RentalPricePerMonth { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(250)]
        public string ImageUrl { get; set; }

        // Foreign Keys
        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; } // ✅ ADD THIS

        // Navigation properties
        public virtual ICollection<FridgeUnit> FridgeUnits { get; set; }
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public virtual ICollection<PurchaseRequestItem> PurchaseRequestItems { get; set; }

        public FridgeModel()
        {
            FridgeUnits = new HashSet<FridgeUnit>();
            PurchaseOrderItems = new HashSet<PurchaseOrderItem>();
            PurchaseRequestItems = new HashSet<PurchaseRequestItem>();
        }
    }
}