using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string ContactEmail { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        // Navigation
        public virtual ICollection<Fridge> Fridges { get; set; }
        public virtual ICollection<Quotation> Quotations { get; set; }
    }
}
