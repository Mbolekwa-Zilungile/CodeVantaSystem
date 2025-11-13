using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class RentedFridge
    {
        [Key]
        public int Id { get; set; }

        // OLD: FridgeId -> NEW: FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }
        [ForeignKey("FridgeUnitId")]
        public FridgeUnit FridgeUnit { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public DateTime RentedAt { get; set; } = DateTime.UtcNow;

        // This tracks if the rental is currently active/deployed
        public bool IsActive { get; set; } = true;

        // RentedFridge.cs
        // ...
        public int RentalOrderId { get; set; } // Foreign Key
        public virtual RentalOrder RentalOrder { get; set; } // <--- ADD THIS LINE// ...
    }
}