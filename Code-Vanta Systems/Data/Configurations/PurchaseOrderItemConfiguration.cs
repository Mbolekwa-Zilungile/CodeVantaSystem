using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
        {
            builder.Property(poi => poi.UnitPrice)
                   .HasColumnType("decimal(18,2)");

            // 🛠️ MODIFIED: Cascade Delete when the PurchaseOrder is removed
            builder.HasOne(poi => poi.PurchaseOrder)
                   .WithMany(po => po.Items)
                   .HasForeignKey(poi => poi.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 🛠️ MODIFIED: Restrict deletion of InventoryItem
            builder.HasOne(poi => poi.InventoryItem)
                   .WithMany(ii => ii.PurchaseOrderItems)
                   .HasForeignKey(poi => poi.InventoryItemId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}