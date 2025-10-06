using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class ItemCatalogConfiguration : IEntityTypeConfiguration<ItemCatalog>
    {
        public void Configure(EntityTypeBuilder<ItemCatalog> builder)
        {
            // Fixes UnitPrice precision warning (standard currency)
            builder.Property(ic => ic.UnitPrice)
                   .HasColumnType("decimal(18,2)");

            // Fixes Category error
            builder.Property(ic => ic.Category)
                   .HasConversion<string>();

            // Fixes Supplier error
            builder.HasOne(ic => ic.Supplier)
                   .WithMany(s => s.ItemCatalogs)
                   .HasForeignKey(ic => ic.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}