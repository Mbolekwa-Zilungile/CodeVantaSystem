using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeRequestConfiguration : IEntityTypeConfiguration<FridgeRequest>
    {
        public void Configure(EntityTypeBuilder<FridgeRequest> builder)
        {
            builder.HasOne(fr => fr.Customer)
                   .WithMany(c => c.FridgeRequests)
                   .HasForeignKey(fr => fr.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fr => fr.Location)
                   .WithMany(l => l.FridgeRequests)
                   .HasForeignKey(fr => fr.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 🛠️ ADDED: Link to the User who submitted the request
            builder.HasOne(fr => fr.RequestedBy)
                   .WithMany(u => u.SubmittedRequests)
                   .HasForeignKey(fr => fr.RequestedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}