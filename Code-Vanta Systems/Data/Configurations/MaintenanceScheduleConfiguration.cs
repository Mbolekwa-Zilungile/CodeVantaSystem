using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
    {
        public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
        {
            // Existing relationship: Link to the Fridge Model
            builder.HasOne(ms => ms.Fridge)
                   .WithMany(f => f.MaintenanceSchedules)
                   .HasForeignKey(ms => ms.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ NEW CRITICAL FIX: Link to the Fridge Unit (The specific physical asset)
            // Assumes the MaintenanceSchedule model has FridgeUnitId and FridgeUnit navigation properties.
            builder.HasOne(ms => ms.FridgeUnit)
                   .WithMany(u => u.MaintenanceSchedules) // Links to the ICollection<MaintenanceSchedule> on FridgeUnit
                   .HasForeignKey(ms => ms.FridgeUnitId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Existing Technician Relationship (If not done elsewhere, include it here)
            // Assuming ApplicationUser has a navigation property back to MaintenanceSchedules
            builder.HasOne(ms => ms.Technician)
                   .WithMany(u => u.MaintenanceSchedules) // Assuming this property exists on ApplicationUser
                   .HasForeignKey(ms => ms.TechnicianId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}