using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeModelConfiguration : IEntityTypeConfiguration<FridgeModel>
    {
        public void Configure(EntityTypeBuilder<FridgeModel> builder)
        {
            builder.Property(fm => fm.RentalPricePerMonth)
                   .HasColumnType("decimal(10,1)");

            // Relationship with Supplier ✅ NOW WORKS
            builder.HasOne(fm => fm.Supplier)
                   .WithMany(s => s.FridgeModels)
                   .HasForeignKey(fm => fm.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Ensure model name is unique
            builder.HasIndex(fm => fm.Name)
                   .IsUnique();
        }
    }
}