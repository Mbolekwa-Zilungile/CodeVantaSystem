using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
    {
        public void Configure(EntityTypeBuilder<ServiceRecord> builder)
        {
            // Configure the link to the Technician
            builder.HasOne(sr => sr.Technician)
                   .WithMany() // Assuming ApplicationUser doesn't have a navigation property back to ServiceRecords
                   .HasForeignKey(sr => sr.TechnicianUserId)
                   .OnDelete(DeleteBehavior.Restrict); // Use Restrict to prevent user deletion cascade

            // Configure the link to the Fridge Unit (The CRITICAL FIX)
            // This links the service record to the specific unit instance.
            builder.HasOne(sr => sr.FridgeUnit)
                   .WithMany(u => u.ServiceRecords) // Links to the ICollection<ServiceRecord> on FridgeUnit
                   .HasForeignKey(sr => sr.FridgeUnitId)
                   .OnDelete(DeleteBehavior.Restrict); // Crucial to prevent cascade loop

            // Configure the link to the Fault (if applicable)
            builder.HasOne(sr => sr.Fault)
                   .WithMany() // Assuming FridgeFault doesn't have a navigation property back to ServiceRecords
                   .HasForeignKey(sr => sr.FaultId)
                   .OnDelete(DeleteBehavior.Restrict); // Use Restrict to prevent fault deletion cascade
        }
    }
}