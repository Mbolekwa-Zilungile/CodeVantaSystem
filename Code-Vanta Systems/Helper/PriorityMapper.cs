using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Models.Enums.Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Helper
{
    public static class PriorityMapper
    {
        public static PriorityLevel ToPriorityLevel(RequestPriority? requestPriority)
        {
            if (!requestPriority.HasValue)
                return PriorityLevel.Medium; // Default value

            return requestPriority.Value switch
            {
                RequestPriority.Low => PriorityLevel.Low,
                RequestPriority.Medium => PriorityLevel.Medium,
                RequestPriority.High => PriorityLevel.High,
                RequestPriority.Urgent => PriorityLevel.Urgent,
                _ => PriorityLevel.Medium
            };
        }

        public static RequestPriority? ToRequestPriority(PriorityLevel priorityLevel)
        {
            return priorityLevel switch
            {
                PriorityLevel.Low => RequestPriority.Low,
                PriorityLevel.Medium => RequestPriority.Medium,
                PriorityLevel.High => RequestPriority.High,
                PriorityLevel.Urgent => RequestPriority.Urgent,
                _ => RequestPriority.Medium
            };
        }
    }
}