using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeAllocationConfiguration : IEntityTypeConfiguration<FridgeAllocation>
    {
        public void Configure(EntityTypeBuilder<FridgeAllocation> builder)
        {
            builder.HasOne(fa => fa.Fridge)
                   .WithMany(f => f.Allocations)
                   .HasForeignKey(fa => fa.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fa => fa.Customer)
                   .WithMany(c => c.Allocations)
                   .HasForeignKey(fa => fa.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
