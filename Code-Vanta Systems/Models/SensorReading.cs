using System;
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class SensorReading
    {
        [Key]
        public int Id { get; set; }

        // CHANGE: FridgeId → FridgeUnitId
        [Required]
        public int FridgeUnitId { get; set; }
        public virtual FridgeUnit FridgeUnit { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public decimal TemperatureCelsius { get; set; }

        public decimal? HumidityPercent { get; set; }

        public bool DoorOpen { get; set; }
    }
}