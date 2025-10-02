using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }

        // Company name
        [Required, StringLength(100)]
        public string Name { get; set; }

        // Contact info (pulled from ApplicationUser)
        [NotMapped]
        public string ContactEmail => User?.Email;

        [NotMapped]
        public string ContactName => User?.FullName;

        [Required, Phone]
        public string Phone { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Fridge> Fridges { get; set; }
        public virtual ICollection<Quotation> Quotations { get; set; }
        public virtual ICollection<DeliveryNote> DeliveryNotes { get; set; }

        // ✅ New: Supplier can have many RequestQuotations
        public virtual ICollection<RequestQuotation> RequestQuotations { get; set; }

        public Supplier()
        {
            Fridges = new HashSet<Fridge>();
            Quotations = new HashSet<Quotation>();
            DeliveryNotes = new HashSet<DeliveryNote>();
            RequestQuotations = new HashSet<RequestQuotation>();
        }
    }
}
