using Code_Vanta_Systems.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class DeliveryNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DeliveryDate { get; set; } = DateTime.Now;

        //Foreign key: Supplier
        [Required]
        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        //Foreign key: PurchaseOrder
        [Required]
        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        //Foreign key: ApplicationUser (Receiver)
        // If receiving user might sometimes be unknown → make it nullable: string?
        [Required]
        public string ReceivedById { get; set; }

        [ForeignKey("ReceivedById")]
        public virtual ApplicationUser ReceivedBy { get; set; }

        //Optional notes
        [StringLength(500)]
        public string Notes { get; set; }
    }
}
