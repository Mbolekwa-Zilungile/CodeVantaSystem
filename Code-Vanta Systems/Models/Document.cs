using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class Document
    {

        public int Id { get; set; }

        // Name of the uploaded file (e.g., "invoice.pdf")
        [Required]
        public string FileName { get; set; }

        // Path to the file location (can be relative or absolute)
        [Required]
        public string FilePath { get; set; }

        // Timestamp indicating when the document was uploaded
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Foreign key referencing the user who uploaded the document
        [Required]
        public string UploadedByUserId { get; set; }

        // Navigation property to the ApplicationUser entity
        // Represents the user who uploaded the document
        [ForeignKey("UploadedByUser")]
        public ApplicationUser UploadedByUser { get; set; }

        // Type of entity this document is related to (e.g "Quotation", "Fault")
        [Required]
        public string RelatedEntityType { get; set; }

        // ID of the related entity (e.g., Quotation ID, Fault ID)
        public int RelatedEntityId { get; set; }

    }
}
