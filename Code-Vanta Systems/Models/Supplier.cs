using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Supplier
    {

        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        // Navigation properties
        public virtual ICollection<Fridge> Fridges { get; set; }
        public virtual ICollection<Quotation> Quotations { get; set; }

        public Supplier()
        {
            Fridges = new HashSet<Fridge>();
            Quotations = new HashSet<Quotation>();


        }
    }
