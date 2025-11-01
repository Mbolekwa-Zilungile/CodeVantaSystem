using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class PurchaseRequestItemConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder)
        {
            builder.HasOne(pri => pri.PurchaseRequest)
                   .WithMany(pr => pr.RequestedItems)
                   .HasForeignKey(pri => pri.PurchaseRequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            // CHANGE: Fridge → FridgeModel (we request models, not specific units)
            builder.HasOne(pri => pri.FridgeModel)
                   .WithMany()
                   .HasForeignKey(pri => pri.FridgeModelId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
