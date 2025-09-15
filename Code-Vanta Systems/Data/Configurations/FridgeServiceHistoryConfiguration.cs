using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeServiceHistoryConfiguration : IEntityTypeConfiguration<FridgeServiceHistory>
    {
        public void Configure(EntityTypeBuilder<FridgeServiceHistory> builder)
        {
            builder.HasOne(fsh => fsh.Fridge)
                   .WithMany(f => f.ServiceHistories)
                   .HasForeignKey(fsh => fsh.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
