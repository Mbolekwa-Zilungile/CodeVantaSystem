using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels.Maintenance_Schedule
{
    public class MaintenanceViewModel
    {
        public int Id { get; set; }
        public int FridgeId { get; set; }
        public string FridgeModel { get; set; }
        public string FridgeSerial { get; set; }
        public int CustomerId { get; set; }
        public string BusinessName { get; set; }

        public string TechnicianId { get; set; }
        public string TechnicianName { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime MaintenanceDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }

        public string MaintenanceDescription { get; set; }
        public MaintenanceStatus Status { get; set; }
    }
}
