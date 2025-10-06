using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    // InventoryItem now only tracks the quantity in stock for a specific catalog item.
    public class InventoryItem
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key to the master definition of the item (e.g., "Compressor Model X")
        [Required]
        public int ItemCatalogId { get; set; }
        public virtual ItemCatalog ItemCatalog { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int QuantityAvailable { get; set; }

        // Optional: Track which supplier last provided this stock batch (if needed)
        public int? SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new HashSet<PurchaseOrderItem>();
    }
}