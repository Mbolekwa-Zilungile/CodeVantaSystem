// PurchaseOrderItemViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.ViewModels.PurchaseOrder
{
    public class PurchaseOrderItemViewModel
    {
        [Required(ErrorMessage = "Fridge model is required")]
        public int FridgeModelId { get; set; } // ADD THIS PROPERTY

        [Required(ErrorMessage = "Item name is required")]
        public string ItemName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        // Optional: If you need to display additional information
        public string Description { get; set; }
    }
}