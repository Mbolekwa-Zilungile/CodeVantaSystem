using System;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeFaultHistoryViewModel
    {
        public string FaultDescription { get; set; }
        public DateTime DateReported { get; set; }
        public FaultSeverity Severity { get; set; } 
        public FaultStatus Status { get; set; }     
    }
}