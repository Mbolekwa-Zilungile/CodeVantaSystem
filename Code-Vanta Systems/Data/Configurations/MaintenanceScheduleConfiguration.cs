using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
    {
        public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
        {
            // CHANGE: Fridge -> FridgeUnit
            builder.HasOne(ms => ms.FridgeUnit)
                   .WithMany(fu => fu.MaintenanceSchedules)
                   .HasForeignKey(ms => ms.FridgeUnitId) // CHANGE: FridgeId -> FridgeUnitId
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}