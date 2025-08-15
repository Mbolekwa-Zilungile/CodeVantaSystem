using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RequestQuotation
    {

        [Key]
        public int RequestQuotationId { get; set; }

        // Date when the quotation request was made
        [Required]
        public DateTime RequestDate { get; set; }

        // Foreign key referencing the user who made the request
        [Required]
        public string RequestedByUserId { get; set; }

        // Navigation property for the requesting user
        [ForeignKey("RequestedByUserId")]
        public ApplicationUser RequestedBy { get; set; }

        // Optional notes/remarks about the quotation request
        [MaxLength(500)]
        public string Notes { get; set; }

        // Collection of quotations linked to this request
        public ICollection<Quotation> Quotations { get; set; }

    }
}
