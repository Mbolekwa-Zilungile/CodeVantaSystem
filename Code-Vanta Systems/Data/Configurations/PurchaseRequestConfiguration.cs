using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
    {
        public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
        {
            builder.HasOne(pr => pr.RequestedBy)
                   .WithMany(u => u.PurchaseRequests)
                   .HasForeignKey(pr => pr.RequestedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
