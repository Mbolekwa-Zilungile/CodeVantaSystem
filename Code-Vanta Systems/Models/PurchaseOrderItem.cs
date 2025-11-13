using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseOrderItem
    {

        [Key]
        public int ItemId { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }

        // OLD: FridgeId -> NEW: FridgeModelId (Linking to the type being ordered)
        [Required]
        public int FridgeModelId { get; set; }
        public FridgeModel FridgeModel { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal Total => Quantity * UnitPrice;
    }
}