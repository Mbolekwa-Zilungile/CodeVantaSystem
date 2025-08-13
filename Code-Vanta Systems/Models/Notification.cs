using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class Notification
    {

        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string RecipientUserId { get; set; }
        public ApplicationUser RecipientUser { get; set; }

    }
}
