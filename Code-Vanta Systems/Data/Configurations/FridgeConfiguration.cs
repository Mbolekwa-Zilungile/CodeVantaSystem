using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeConfiguration : IEntityTypeConfiguration<Fridge>
    {
        public void Configure(EntityTypeBuilder<Fridge> builder)
        {
            builder.Property(f => f.Status)
                   .HasConversion<string>();

            // Existing configuration for Location
            builder.HasOne(f => f.Location)
                   .WithMany(l => l.Fridges)
                   .HasForeignKey(f => f.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Existing configuration for Supplier
            builder.HasOne(f => f.Supplier)
                   .WithMany(s => s.Fridges)
                   .HasForeignKey(f => f.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // FIX: Configuration for TechnicianUserId (to prevent cascade cycle)
            builder.HasOne(f => f.Technician)
                   .WithMany() // ApplicationUser can have many Fridges, but no navigation property is defined in ApplicationUser
                   .HasForeignKey(f => f.TechnicianUserId)
                   .OnDelete(DeleteBehavior.Restrict); // <-- CRITICAL FIX: Sets ON DELETE NO ACTION
        }
    }
}