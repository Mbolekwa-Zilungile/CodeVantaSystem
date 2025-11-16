using System.ComponentModel.DataAnnotations;
using Code_Vanta_Systems.Models.Enums; // Assuming FaultSeverity is accessible

namespace Code_Vanta_Systems.ViewModels
{
    public class FaultLocationViewModel
    {
        public int FaultId { get; set; }
        public string UnitSerial { get; set; }
        public string Severity { get; set; }

        // Fields that hold the location details
        public string Address { get; set; }

        // Optional fields if you have geo-coordinates in your Location model
        // public decimal? Latitude { get; set; }
        // public decimal? Longitude { get; set; }
    }
}