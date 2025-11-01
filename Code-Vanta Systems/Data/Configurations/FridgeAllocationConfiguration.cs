using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeAllocationConfiguration : IEntityTypeConfiguration<FridgeAllocation>
    {
        public void Configure(EntityTypeBuilder<FridgeAllocation> builder)
        {
            // CHANGE: Fridge → FridgeUnit
            builder.HasOne(fa => fa.FridgeUnit)
                   .WithMany(f => f.Allocations)
                   .HasForeignKey(fa => fa.FridgeUnitId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fa => fa.Customer)
                   .WithMany(c => c.Allocations)
                   .HasForeignKey(fa => fa.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fa => fa.CreatedBy)
                   .WithMany(u => u.CreatedAllocations)
                   .HasForeignKey(fa => fa.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}