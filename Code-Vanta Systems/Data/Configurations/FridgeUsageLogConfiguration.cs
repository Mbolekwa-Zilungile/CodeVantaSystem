using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeUsageLogConfiguration : IEntityTypeConfiguration<FridgeUsageLog>
    {
        public void Configure(EntityTypeBuilder<FridgeUsageLog> builder)
        {
            // CHANGE: Fridge → FridgeUnit
            builder.HasOne(fu => fu.FridgeUnit)
                   .WithMany(f => f.UsageLogs)
                   .HasForeignKey(fu => fu.FridgeUnitId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
