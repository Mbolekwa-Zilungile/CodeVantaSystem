using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class SensorReading
    {

        public int ReadingId { get; set; }

        // Foreign key referencing the fridge where the reading was taken
        [Required]
        public int FridgeId { get; set; }

        // Navigation property for the fridge
        [ForeignKey("FridgeId")]
        public Fridge Fridge { get; set; }

        // Date and time when the reading was recorded
        [Required]
        public DateTime ReadingDate { get; set; }

        // Temperature reading in degrees Celsius
        [Range(-50, 100)]
        public double Temperature { get; set; }

        // Humidity reading as a percentage
        [Range(0, 100)]
        public double Humidity { get; set; }

        // Number of times the fridge door was opened
        [Range(0, int.MaxValue)]
        public int DoorOpenCount { get; set; }

        // Indicates whether an alert was triggered based on the reading
        public bool AlertTriggered { get; set; }

    }
}
