using System;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class SensorReading
    {
        [Key] // <--- FIX: This explicitly defines the Primary Key
        public int Id { get; set; }

        // Foreign Key to the Fridge
        [Required]
        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public decimal TemperatureCelsius { get; set; }

        public decimal? HumidityPercent { get; set; }

        public bool DoorOpen { get; set; }
    }
}