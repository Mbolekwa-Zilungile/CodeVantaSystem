using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels.DeliveryNote
{
    public class DeliveryNoteDetailsViewModel
    {
        public int Id { get; set; } // The Primary Key of the DeliveryNote entity
        public string DeliveryNoteNumber { get; set; } // Assuming you generate this number in the controller
        public DateTime DeliveryDate { get; set; }
        public DateTime? GeneratedDate { get; set; }
        public bool IsApproved { get; set; }
        public string Status { get; set; } // Derived from IsApproved or a Status field if you add one
        public string Notes { get; set; }

        // --- Receiver Information (from ApplicationUser) ---
        public string ReceivedByName { get; set; }
        public string ReceivedByEmail { get; set; }

        // --- Supplier/Sender Information (Fetched via PO -> Supplier) ---
        public string SupplierName { get; set; }
        public string SupplierEmail { get; set; }
        public string SupplierPhone { get; set; }
        public string SupplierAddress { get; set; }

        // --- Purchase Order Reference Information ---
        public string PONumber { get; set; }
        public string DeliveryAddress { get; set; }
        public string DeliveryTerms { get; set; }
        public string PaymentTerms { get; set; }

        // --- Items ---
        // Using the PurchaseOrderItem entity/model for line items
        public List<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

        // --- Calculated Total ---
        public decimal GrandTotal => Items.Sum(i => i.Quantity * i.UnitPrice);
    }
}
