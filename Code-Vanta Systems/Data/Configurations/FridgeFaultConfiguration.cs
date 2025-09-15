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

            builder.HasOne(ff => ff.AssignedTechnician)
                   .WithMany(u => u.AssignedFaults)
                   .HasForeignKey(ff => ff.AssignedTechnicianId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
