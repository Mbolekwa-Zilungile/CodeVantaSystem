using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int QuantityAvailable { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        // Optional: Track which supplier provided the item
        public int? SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        // Navigation property for purchase order items
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new HashSet<PurchaseOrderItem>();
    }
}
