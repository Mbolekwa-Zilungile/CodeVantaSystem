namespace Code_Vanta_Systems.ViewModels.DeliveryNote
{
    public class DeliveryItemViewModel
    {
        public string FridgeModel { get; set; }    // 👈 This is the property your view needs
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}