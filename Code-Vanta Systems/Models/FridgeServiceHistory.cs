using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeServiceHistory
    {
        public int Id { get; set; }

        // Foreign key referencing the fridge that was serviced
        public int FridgeId { get; set; }

        // Navigation property for the fridge
        public virtual Fridge Fridge { get; set; }

        // Date when the service was performed
        public DateTime ServiceDate { get; set; }


        // Description of the service performed
        [Required, StringLength(200)]
        public string ServiceDescription { get; set; }

        // Foreign key referencing the technician who performed the service
        public string TechnicianId { get; set; }

        // Navigation property for the technician
        public virtual ApplicationUser Technician { get; set; }
    }
}
