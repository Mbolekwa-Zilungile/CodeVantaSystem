using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Supplier
    {

        [Key]
        public int Id { get; set; }

        // Name of the supplier
        [Required, StringLength(100)]
        public string Name { get; set; }

        // Contact email address of the supplier
        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; }

        // Contact phone number of the supplier
        [Required]
        [Phone]
        public string Phone { get; set; }

        // Physical or mailing address of the supplier
        [Required, StringLength(300)]
        public string Address { get; set; }

        // Navigation property for fridges supplied by this supplier
        public virtual ICollection<Fridge> Fridges { get; set; }

        // Navigation property for quotations provided by this supplier
        public virtual ICollection<Quotation> Quotations { get; set; }


        // Constructor to initialize collections
        public Supplier()
        {
            Fridges = new HashSet<Fridge>();
            Quotations = new HashSet<Quotation>();


        }
    }
}