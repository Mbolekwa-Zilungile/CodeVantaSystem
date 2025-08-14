using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class AuditTrail
    {

        [Key]
        public int AuditId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; }

        [Required]
        public int EntityId { get; set; }

        [Required]
        public string ChangedByUserId { get; set; }

        [ForeignKey("ChangedByUserId")]
        public ApplicationUser ChangedByUser { get; set; }

        [Required]
        [MaxLength(20)]
        public string ChangeType { get; set; } // Create, Update, Delete

        [Required]
        public DateTime ChangeDate { get; set; }

        public string ChangeDetails { get; set; } // JSON or text

    }
}
