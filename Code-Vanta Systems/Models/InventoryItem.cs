using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        // Name of the inventory item
        [Required]
        public string Name { get; set; }

        // Optional description of the item
        public string Description { get; set; }

        // Quantity currently available in stock
        public int QuantityAvailable { get; set; }


        // Unit price of the item with decimal precision

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation property for purchase order items that include this inventory item
        public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        // Constructor to initialize the collection
        public InventoryItem()
        {
            PurchaseOrderItems = new HashSet<PurchaseOrderItem>();
        }


    }
}
