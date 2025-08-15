using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseOrderItem
    {

        [Key]
        public int ItemId { get; set; }

        // Foreign key referencing the associated purchase order
        [Required]
        public int PurchaseOrderId { get; set; }

        // Navigation property for the purchase order
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder PurchaseOrder { get; set; }

        // Foreign key referencing the inventory item being ordered
        [Required]
        public int InventoryItemId { get; set; }

        // Navigation property for the inventory item
        [ForeignKey("InventoryItemId")]
        public InventoryItem InventoryItem { get; set; }

        // Quantity of the inventory item ordered
        [Required]
        public int Quantity { get; set; }

        // Unit price of the inventory item
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

    }
}
