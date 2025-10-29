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

        [Required]
        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}
