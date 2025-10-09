using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RequestQuotation
    {
        [Key]
        public int RequestQuotationId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string RFQId { get; set; } // <-- Add this property

        [Required]
        public string RequestedByUserId { get; set; }
        [ForeignKey("RequestedByUserId")]
        public ApplicationUser RequestedBy { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public ICollection<Quotation> Quotations { get; set; } = new HashSet<Quotation>();
    }
}
