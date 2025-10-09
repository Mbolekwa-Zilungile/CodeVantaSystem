using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RentedFridge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FridgeId { get; set; }
        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public DateTime RentedAt { get; set; } = DateTime.UtcNow;
    }
}
