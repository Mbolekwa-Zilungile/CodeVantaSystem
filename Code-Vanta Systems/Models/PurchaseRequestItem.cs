using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models
{
    public class PurchaseRequestItem
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public int PurchaseRequestId { get; set; }
        public PurchaseRequest PurchaseRequest { get; set; }

        // OLD: FridgeId -> NEW: FridgeModelId (Linking to the type being requested)
        [Required]
        public int FridgeModelId { get; set; }
        public FridgeModel FridgeModel { get; set; }

        // ModelRequested and Description are now likely redundant but kept for history
        public string ModelRequested { get; set; }
        public string Description { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}