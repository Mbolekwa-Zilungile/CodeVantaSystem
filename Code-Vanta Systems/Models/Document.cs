using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Document
    {

        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; } // Store relative or absolute path

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UploadedByUserId { get; set; }
        public ApplicationUser UploadedByUser { get; set; }

        [Required]
        public string RelatedEntityType { get; set; } // e.g., "Quotation", "Fault"

        public int RelatedEntityId { get; set; }

    }
}
