using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeUnitConfiguration : IEntityTypeConfiguration<FridgeUnit>
    {
        public void Configure(EntityTypeBuilder<FridgeUnit> builder)
        {
            builder.Property(fu => fu.Status)
                   .HasConversion<string>();

            builder.Property(fu => fu.Condition)
                   .HasConversion<string>();

            // Relationship with FridgeModel
            builder.HasOne(fu => fu.FridgeModel)
                   .WithMany(fm => fm.FridgeUnits)
                   .HasForeignKey(fu => fu.FridgeModelId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Customer (current allocation)
            builder.HasOne(fu => fu.CurrentCustomer)
                   .WithMany()
                   .HasForeignKey(fu => fu.CurrentCustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Technician
            builder.HasOne(fu => fu.Technician)
                   .WithMany()
                   .HasForeignKey(fu => fu.TechnicianUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Location ✅ ADD THIS
            builder.HasOne(fu => fu.Location)
                   .WithMany(l => l.FridgeUnits)
                   .HasForeignKey(fu => fu.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Ensure SerialNumber is unique
            builder.HasIndex(fu => fu.SerialNumber)
                   .IsUnique();
        }
    }
}