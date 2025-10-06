using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class TechnicianScheduleConfiguration : IEntityTypeConfiguration<TechnicianSchedule>
    {
        public void Configure(EntityTypeBuilder<TechnicianSchedule> builder)
        {
            // Relationship: TechnicianSchedule (Many) → Technician (ApplicationUser)
            builder.HasOne(ts => ts.Technician)
                   .WithMany(u => u.TechnicianSchedules)
                   .HasForeignKey(ts => ts.TechnicianUserId) // Using TechnicianUserId from your model
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship: TechnicianSchedule (Many) → Location (One)
            builder.HasOne(ts => ts.Location)
                   .WithMany(l => l.ScheduledVisits)
                   .HasForeignKey(ts => ts.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}