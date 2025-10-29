namespace Code_Vanta_Systems.ViewModels.PurchaseOrder
{
    public class PurchaseOrderItemViewModel
    {
        public int InventoryItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
