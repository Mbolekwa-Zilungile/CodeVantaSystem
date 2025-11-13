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

            // CHANGE: Link to Fridge is now a link to FridgeUnit
            builder.HasOne(fm => fm.FridgeUnit) // Change: fm.Fridge -> fm.FridgeUnit 
                   .WithMany(fu => fu.Maintenances)
                   .HasForeignKey(fm => fm.FridgeUnitId) // Change: fm.FridgeId -> fm.FridgeUnitId
                   .OnDelete(DeleteBehavior.Restrict);

            // Existing Technician Relationship
            builder.HasOne(fm => fm.Technician)
                   .WithMany(u => u.AssignedMaintenances)
                   .HasForeignKey(fm => fm.TechnicianId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}