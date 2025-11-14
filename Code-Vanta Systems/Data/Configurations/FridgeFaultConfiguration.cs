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

            // Existing Assigned Technician Relationship
            builder.HasOne(ff => ff.AssignedTechnician)
                   .WithMany(u => u.AssignedFaults)
                   .HasForeignKey(ff => ff.AssignedTechnicianId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 🛠️ ADDED: Link to the User who Reported the fault
            builder.HasOne(ff => ff.ReportedBy)
                   .WithMany(u => u.ReportedFaults)
                   .HasForeignKey(ff => ff.ReportedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ FIX: NEW RELATIONSHIP TO FRIDGE UNIT
            // This links the fault to the specific unit instance.
            // Using OnDelete(DeleteBehavior.Restrict) prevents the "multiple cascade paths" error.
            builder.HasOne(ff => ff.FridgeUnit)
                   .WithMany() // Assuming FridgeUnit doesn't have a navigation property back to FridgeFaults
                   .HasForeignKey(ff => ff.FridgeUnitId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}