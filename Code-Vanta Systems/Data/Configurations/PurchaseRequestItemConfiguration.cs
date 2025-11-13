using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class PurchaseRequestItemConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder)
        {
            // Relationship: PurchaseRequestItem → PurchaseRequest (remains the same)
            builder.HasOne(pri => pri.PurchaseRequest)
                   .WithMany(pr => pr.RequestedItems)
                   .HasForeignKey(pri => pri.PurchaseRequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            // CHANGE: Fridge -> FridgeModel (for purchasing a type/specification)
            builder.HasOne(pri => pri.FridgeModel) // CHANGE: pri.Fridge -> pri.FridgeModel
                   .WithMany()
                   .HasForeignKey(pri => pri.FridgeModelId) // CHANGE: pri.FridgeId -> pri.FridgeModelId
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}