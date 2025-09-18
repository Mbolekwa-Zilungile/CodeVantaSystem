using Code_Vanta_Systems.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        [Required]
        public int LocationId { get; set; }
        public virtual Location Location { get; set; }

        // Who submitted the request (Customer or Liaison)
        [Required]
        public string RequestedByUserId { get; set; }
        public virtual ApplicationUser RequestedBy { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string Notes { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
    }
}
