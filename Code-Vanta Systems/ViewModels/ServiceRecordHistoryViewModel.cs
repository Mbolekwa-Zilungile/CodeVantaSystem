using System;

namespace Code_Vanta_Systems.ViewModels
{
    public class ServiceRecordHistoryViewModel
    {
        public DateTime DateOfService { get; set; }
        public string ServiceDetails { get; set; }
        public int? FaultId { get; set; } // Links back to the fault, if resolved
    }
}