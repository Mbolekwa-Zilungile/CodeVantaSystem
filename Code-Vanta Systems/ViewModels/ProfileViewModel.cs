using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class ProfileViewModel
    { 
        public required string BusinessName { get; set; }
        public required string BusinessType { get; set; }
        public required string ContactName { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public required string PhysicalAddress { get; set; }
        public required Location Location { get; set; } 
        public required int RentalsCount { get; set; }
        public required int PendingOrders { get; set; }
        public required int ApprovedOrders { get; set; }
        public required int RejectedOrders { get; set; }
        public required int PaidOrders { get; set; }
        public required int UnPaidOrders { get; set; } 
    }
}
