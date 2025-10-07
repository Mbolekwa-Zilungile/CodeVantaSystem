using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class PurchaseRequestItemConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder)
        {
            // Relationship: PurchaseRequestItem → PurchaseRequest
            builder.HasOne(pri => pri.PurchaseRequest)
                   .WithMany(pr => pr.RequestedItems)
                   .HasForeignKey(pri => pri.PurchaseRequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Relationship: PurchaseRequestItem → Fridge (instead of ItemCatalog)
            builder.HasOne(pri => pri.Fridge)
                   .WithMany()
                   .HasForeignKey(pri => pri.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
