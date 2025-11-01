using Code_Vanta_Systems.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PurchaseOrderItem
{
    [Key]
    public int ItemId { get; set; }

    [Required]
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }

    // CHANGE: FridgeId → FridgeModelId (we're ordering models, not specific units)
    [Required]
    public int FridgeModelId { get; set; }
    public FridgeModel FridgeModel { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal Total => Quantity * UnitPrice;
}