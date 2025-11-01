using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeMaintenanceConfiguration : IEntityTypeConfiguration<FridgeMaintenance>
    {
        public void Configure(EntityTypeBuilder<FridgeMaintenance> builder)
        {
            builder.Property(fm => fm.Status)
                   .HasConversion<string>();

            // CHANGE: Fridge → FridgeUnit
            builder.HasOne(fm => fm.FridgeUnit)
                   .WithMany(f => f.Maintenances)
                   .HasForeignKey(fm => fm.FridgeUnitId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(fm => fm.Technician)
                   .WithMany(u => u.AssignedMaintenances)
                   .HasForeignKey(fm => fm.TechnicianId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}