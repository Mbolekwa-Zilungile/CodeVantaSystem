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

            builder.HasOne(f => f.Location)
                   .WithMany(l => l.Fridges)
                   .HasForeignKey(f => f.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Supplier)
                   .WithMany(s => s.Fridges)
                   .HasForeignKey(f => f.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // NEW CRITICAL FIX: TechnicianUserId relationship configuration
            // This explicitly sets ON DELETE NO ACTION (Restrict) to prevent the SQL cascade cycle error (Error 1785)
            builder.HasOne(f => f.Technician)
                   .WithMany() // Assumes ApplicationUser does not have a navigation property back to Fridges
                   .HasForeignKey(f => f.TechnicianUserId)
                   .OnDelete(DeleteBehavior.Restrict); // <-- CRITICAL FIX
        }
    }
}
