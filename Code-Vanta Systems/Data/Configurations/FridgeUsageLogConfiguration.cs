using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeUsageLogConfiguration : IEntityTypeConfiguration<FridgeUsageLog>
    {
        public void Configure(EntityTypeBuilder<FridgeUsageLog> builder)
        {
            builder.HasOne(fu => fu.Fridge)
                   .WithMany(f => f.UsageLogs)
                   .HasForeignKey(fu => fu.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
