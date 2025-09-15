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

            builder.HasOne(poi => poi.PurchaseOrder)
                   .WithMany(po => po.Items)
                   .HasForeignKey(poi => poi.PurchaseOrderId);

            builder.HasOne(poi => poi.InventoryItem)
                   .WithMany(ii => ii.PurchaseOrderItems)
                   .HasForeignKey(poi => poi.InventoryItemId);
        }
    }
}
