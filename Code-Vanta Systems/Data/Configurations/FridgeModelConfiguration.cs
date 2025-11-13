using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class FridgeModelConfiguration : IEntityTypeConfiguration<FridgeModel>
    {
        public void Configure(EntityTypeBuilder<FridgeModel> builder)
        {
            // 1. Configure precision for the decimal field based on the [Precision(10, 2)] attribute
            builder.Property(fm => fm.RentalPricePerMonth)
                   .HasColumnType("decimal(10,2)");

            // 2. Configure enum conversion to store the FridgeCategory as a string
            builder.Property(fm => fm.Category)
                   .HasConversion<string>();

            // 3. Configure the one-to-many relationship with the Supplier
            // FridgeModel (Many) -> Supplier (One)
            // It uses SupplierId as the foreign key.
            builder.HasOne(fm => fm.Supplier)
                   .WithMany(s => s.FridgeModels) // Assuming the Supplier model has a collection named 'FridgeModels'
                   .HasForeignKey(fm => fm.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}