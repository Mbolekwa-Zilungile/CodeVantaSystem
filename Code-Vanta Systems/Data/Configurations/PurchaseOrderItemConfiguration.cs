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

            // Relationship: PurchaseOrderItem (Many) → PurchaseOrder (One)
            // MODIFIED: Cascade Delete when the PurchaseOrder is removed
            builder.HasOne(poi => poi.PurchaseOrder)
                   .WithMany(po => po.Items)
                   .HasForeignKey(poi => poi.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Relationship: PurchaseOrderItem (Many) → FridgeModel (One)
            // It links to FridgeModel (for the type/specification being purchased).
            // Since FridgeModel does not contain a navigation collection for PurchaseOrderItems, we use WithMany().
            builder.HasOne(poi => poi.FridgeModel)
                   .WithMany() // Correctly uses WithMany() as FridgeModel only has a 'Units' collection.
                   .HasForeignKey(poi => poi.FridgeModelId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}