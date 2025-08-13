namespace Code_Vanta_Systems.Models
{
    public class FridgeAllocation
    {
        public int Id { get; set; }

        public int FridgeId { get; set; }
        public virtual Fridge Fridge { get; set; }

        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        public DateTime AllocationDate { get; set; }
        public DateTime? DeallocationDate { get; set; }
    }
}
