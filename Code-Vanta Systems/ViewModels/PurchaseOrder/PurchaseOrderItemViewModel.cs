namespace Code_Vanta_Systems.ViewModels.PurchaseOrder
{
    public class PurchaseOrderItemViewModel
    {
        public int FridgeId { get; set; }

        public string ItemName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        // Calculated property
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
