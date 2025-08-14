using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class SensorReading
    {

        public int ReadingId { get; set; }

        [Required]
        public int FridgeId { get; set; }

        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        [Required]
        public DateTime ReadingDate { get; set; }

        [Range(-50, 100)]
        public double Temperature { get; set; }

        [Range(0, 100)]
        public double Humidity { get; set; }

        [Range(0, int.MaxValue)]
        public int DoorOpenCount { get; set; }

        public bool AlertTriggered { get; set; }

    }
}
