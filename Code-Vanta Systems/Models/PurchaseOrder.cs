using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseOrder
    {

        [Key]
        public int PurchaseOrderId { get; set; }

        // Date when the order was placed
        [Required]
        public DateTime OrderDate { get; set; }

        // Foreign key referencing the supplier
        [Required]
        public int SupplierId { get; set; }

        // Navigation property for the supplier
        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; }

        // Foreign key referencing the user who placed the order
        [Required]
        public string OrderedByUserId { get; set; }

        [ForeignKey("OrderedByUserId")]
        public ApplicationUser OrderedBy { get; set; }

        // Status of the order (e.g., Pending, Approved, Delivered, Cancelled)
        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        // Collection of items included in the purchase order
        public ICollection<PurchaseOrderItem> Items { get; set; }

        // Constructor to initialize the collection
        public PurchaseOrder()
        {
            Items = new HashSet<PurchaseOrderItem>();
        }


    }
}
