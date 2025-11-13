using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeUsageLogConfiguration : IEntityTypeConfiguration<FridgeUsageLog>
    {
        public void Configure(EntityTypeBuilder<FridgeUsageLog> builder)
        {
            // CHANGE: Fridge -> FridgeUnit
            builder.HasOne(ful => ful.FridgeUnit)
                   .WithMany(fu => fu.UsageLogs)
                   .HasForeignKey(ful => ful.FridgeUnitId) // CHANGE: FridgeId -> FridgeUnitId
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}