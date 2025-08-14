using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RequestQuotation
    {

        [Key]
        public int RequestQuotationId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public string RequestedByUserId { get; set; }

        [ForeignKey("RequestedByUserId")]
        public ApplicationUser RequestedBy { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public ICollection<Quotation> Quotations { get; set; }

    }
}
