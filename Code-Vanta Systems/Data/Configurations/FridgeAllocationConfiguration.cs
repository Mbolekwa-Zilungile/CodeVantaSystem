using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeAllocationConfiguration : IEntityTypeConfiguration<FridgeAllocation>
    {
        public void Configure(EntityTypeBuilder<FridgeAllocation> builder)
        {
            // 1. Link FridgeAllocation to FridgeUnit
            builder.HasOne(fa => fa.FridgeUnit)
                   .WithMany(fu => fu.Allocations)
                   .HasForeignKey(fa => fa.FridgeUnitId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 2. Link FridgeAllocation to Customer
            builder.HasOne(fa => fa.Customer)
                   .WithMany(c => c.Allocations)
                   .HasForeignKey(fa => fa.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 3. Link FridgeAllocation to the Creating User (ApplicationUser)
            builder.HasOne(fa => fa.CreatedBy)
                   .WithMany(u => u.CreatedAllocations)
                   .HasForeignKey(fa => fa.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}