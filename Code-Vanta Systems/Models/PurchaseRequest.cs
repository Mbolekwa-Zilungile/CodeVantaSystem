using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public string RequestedByUserId { get; set; }
        public virtual ApplicationUser RequestedBy { get; set; }

        [Required, StringLength(100)]
        public string ModelRequested { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public virtual ICollection<Quotation> Quotations { get; set; } = new HashSet<Quotation>();
    }
}
