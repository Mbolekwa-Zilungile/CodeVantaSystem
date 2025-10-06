using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // Relationship: Customer (Many) → Location (One)
            builder.HasOne(c => c.Location)
                   .WithMany(l => l.Customers)
                   .HasForeignKey(c => c.LocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Customer (One) ↔ ApplicationUser (One)
            // The HasForeignKey on ApplicationUserConfiguration already sets up the FK, but this ensures integrity
            builder.HasOne(c => c.User)
                   .WithOne(u => u.CustomerProfile)
                   .HasForeignKey<Customer>(c => c.UserId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}