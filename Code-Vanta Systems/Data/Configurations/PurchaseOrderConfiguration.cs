using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.Property(po => po.Status)
                   .HasConversion<string>();

            // Relationship: PurchaseOrder (Many) → Supplier (One)
            builder.HasOne(po => po.Supplier)
                   .WithMany(s => s.PurchaseOrders)
                   .HasForeignKey(po => po.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship: PurchaseOrder (Many) → OrderedBy (ApplicationUser)
            builder.HasOne(po => po.OrderedBy)
                   .WithMany(u => u.PlacedOrders)
                   .HasForeignKey(po => po.OrderedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}