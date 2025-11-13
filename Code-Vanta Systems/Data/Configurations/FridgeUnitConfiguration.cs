using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeUnitConfiguration : IEntityTypeConfiguration<FridgeUnit>
    {
        public void Configure(EntityTypeBuilder<FridgeUnit> builder)
        {
            // Property conversion for the Status enum
            builder.Property(fu => fu.Status)
                   .HasConversion<string>();

            // FIX: Corrected 'FridgeUnits' to 'Units' based on FridgeModel.cs
            builder.HasOne(fu => fu.FridgeModel)
                   .WithMany(fm => fm.Units)
                   .HasForeignKey(fu => fu.FridgeModelId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Updated relationship: FridgeUnit -> Location
            builder.HasOne(fu => fu.Location)
                   .WithMany(l => l.FridgeUnits)
                   .HasForeignKey(fu => fu.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Existing Technician relationship
            builder.HasOne(fu => fu.Technician)
                   .WithMany()
                   .HasForeignKey(fu => fu.TechnicianUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}