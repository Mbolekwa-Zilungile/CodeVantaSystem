using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Code_Vanta_Systems.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [NotMapped]
        public string ContactEmail => User?.Email;

        [NotMapped]
        public string ContactName => User?.FullName;

        [Required, Phone]
        public string Phone { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;

        // OLD: Fridges -> NEW: FridgeModels
        public virtual ICollection<FridgeModel> FridgeModels { get; set; }
        public virtual ICollection<Quotation> Quotations { get; set; }
        public virtual ICollection<DeliveryNote> DeliveryNotes { get; set; }
        public virtual ICollection<RequestQuotation> RequestQuotations { get; set; }
        public virtual ICollection<ItemCatalog> ItemCatalogs { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }


        public Supplier()
        {
            FridgeModels = new HashSet<FridgeModel>(); // Updated initialization
            Quotations = new HashSet<Quotation>();
            DeliveryNotes = new HashSet<DeliveryNote>();
            RequestQuotations = new HashSet<RequestQuotation>();
            PurchaseOrders = new HashSet<PurchaseOrder>();
            ItemCatalogs = new HashSet<ItemCatalog>();
        }
    }
}