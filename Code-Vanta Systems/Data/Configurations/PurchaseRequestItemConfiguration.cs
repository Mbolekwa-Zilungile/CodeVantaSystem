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
                   .WithMany(pr => pr.RequestedItems) // Assuming 'RequestedItems' is the collection on PurchaseRequest
                   .HasForeignKey(pri => pri.PurchaseRequestId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Relationship: PurchaseRequestItem → ItemCatalog
            builder.HasOne(pri => pri.ItemCatalog)
                   .WithMany()
                   .HasForeignKey(pri => pri.ItemCatalogId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}