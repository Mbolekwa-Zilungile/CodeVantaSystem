using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
    {
        public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
        {
            builder.HasOne(ms => ms.Fridge)
                   .WithMany(f => f.MaintenanceSchedules)
                   .HasForeignKey(ms => ms.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
