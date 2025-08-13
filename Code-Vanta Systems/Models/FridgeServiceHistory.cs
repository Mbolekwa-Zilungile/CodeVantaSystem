using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class FridgeServiceHistory
    {
        public int Id { get; set; }

        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        public DateTime ServiceDate { get; set; }

        [Required, StringLength(200)]
        public string ServiceDescription { get; set; }

        public string TechnicianId { get; set; }
        public virtual ApplicationUser Technician { get; set; }
    }
}
