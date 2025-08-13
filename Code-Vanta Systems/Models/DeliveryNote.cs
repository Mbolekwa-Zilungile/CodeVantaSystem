using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class DeliveryNote
    {
        public int Id { get; set; }

        public DateTime DeliveryDate { get; set; }

        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public int PurchaseRequestId { get; set; }
        public virtual PurchaseRequest PurchaseRequest { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }
}
