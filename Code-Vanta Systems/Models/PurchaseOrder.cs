using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseOrder
    {

        [Key]
        public int PurchaseOrderId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; }

        [Required]
        public string OrderedByUserId { get; set; }

        [ForeignKey("OrderedByUserId")]
        public ApplicationUser OrderedBy { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // Pending, Approved, Delivered, Cancelled

        public ICollection<PurchaseOrderItem> Items { get; set; }


        public PurchaseOrder()
        {
            Items = new HashSet<PurchaseOrderItem>();
        }


    }
}
