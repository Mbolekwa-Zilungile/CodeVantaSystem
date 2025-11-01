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
                   .HasForeignKey(poi => poi.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // CHANGE: Fridge → FridgeModel (we order models, not specific units)
            builder.HasOne(poi => poi.FridgeModel)
                   .WithMany(f => f.PurchaseOrderItems)
                   .HasForeignKey(poi => poi.FridgeModelId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}