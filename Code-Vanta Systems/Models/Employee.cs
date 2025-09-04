using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public EmployeeRole Role { get; set; }

        public int? LocationId { get; set; }
        public virtual Location Location { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}