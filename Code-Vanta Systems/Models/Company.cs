using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; } = 1; // fixed ID

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "Codevanta Systems Ltd.";

        [EmailAddress]
        public string Email { get; set; } = "codevanta@gmail.com";

        [Phone]
        public string Phone { get; set; } = "+27 83 193 9887";

        [StringLength(200)]
        public string Address { get; set; } = "123 Western Way, Port Elizabeth, South Africa";

        public string? RegistrationNumber { get; set; } = "1234567890";
        public string? VatNumber { get; set; } = "VAT123456";
    }
}
