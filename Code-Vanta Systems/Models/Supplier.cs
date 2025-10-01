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

        // Name of the supplier company
        [Required, StringLength(100)]
        public string Name { get; set; }

        //Contact email is stored in the associated ApplicationUser(User.Email)
        [NotMapped]
        public string ContactEmail => User?.Email;

        [NotMapped]
        public string ContactName => User?.FullName;

        // Contact phone number of the supplier
        [Required, Phone]
        public string Phone { get; set; }

        // Physical or mailing address of the supplier
        [Required, StringLength(300)]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;


        // Navigation property for fridges supplied by this supplier
        public virtual ICollection<Fridge> Fridges { get; set; }

        // Navigation property for quotations provided by this supplier
        public virtual ICollection<Quotation> Quotations { get; set; }

        // New: Navigation property for delivery notes
        public virtual ICollection<DeliveryNote> DeliveryNotes { get; set; }

        // Constructor to initialize collections
        public Supplier()
        {
            Fridges = new HashSet<Fridge>();
            Quotations = new HashSet<Quotation>();
            DeliveryNotes = new HashSet<DeliveryNote>();
        }
    }
}
