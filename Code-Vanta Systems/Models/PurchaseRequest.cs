using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseRequest
    {
        [Key]
        public int Id { get; set; }

        public DateTime RequestDate { get; set; }

        [Required]  
        public string RequestedByUserId { get; set; }

        public virtual ApplicationUser RequestedBy { get; set; }

        [Required] 
        [StringLength(100)]
        public string ModelRequested { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public RequestStatus Status { get; set; }

        public virtual ICollection<Quotation> Quotations { get; set; }
    }
}
