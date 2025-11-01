using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeFaultConfiguration : IEntityTypeConfiguration<FridgeFault>
    {
        public void Configure(EntityTypeBuilder<FridgeFault> builder)
        {
            builder.Property(ff => ff.Status)
                   .HasConversion<string>();

            // CHANGE: Add FridgeUnit relationship (NEW)
            builder.HasOne(ff => ff.FridgeUnit)
                   .WithMany(f => f.Faults)
                   .HasForeignKey(ff => ff.FridgeUnitId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ff => ff.AssignedTechnician)
                   .WithMany(u => u.AssignedFaults)
                   .HasForeignKey(ff => ff.AssignedTechnicianId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ff => ff.ReportedBy)
                   .WithMany(u => u.ReportedFaults)
                   .HasForeignKey(ff => ff.ReportedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}