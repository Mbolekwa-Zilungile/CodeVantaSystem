using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Notification
    {


        [Key]
        public int Id { get; set; }

        // The message content of the notification
        [Required]
        public string Message { get; set; }

        // Indicates whether the notification has been read
        public bool IsRead { get; set; } = false;

        // Timestamp when the notification was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key referencing the recipient user
        [Required]
        public string RecipientUserId { get; set; }

        // Navigation property for the recipient user
        public ApplicationUser RecipientUser { get; set; }

    }
}
