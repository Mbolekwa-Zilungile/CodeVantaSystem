using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RentalOrder
    {
        public int? Id { get; set; }
        public string CustomerName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal VAT { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeliveryFee { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public List<RentalOrderItem> Items { get; set; } = new();
    }
}
