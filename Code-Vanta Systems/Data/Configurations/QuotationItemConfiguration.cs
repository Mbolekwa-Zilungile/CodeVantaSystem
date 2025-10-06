using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class QuotationItemConfiguration : IEntityTypeConfiguration<QuotationItem>
    {
        public void Configure(EntityTypeBuilder<QuotationItem> builder)
        {
            // Set decimal precision (Good practice, often defined in the Item entity itself)
            builder.Property(qi => qi.QuotedUnitPrice)
                   .HasColumnType("decimal(18,2)");

            // FIX: Set the relationship between Quotation and QuotationItem to RESTRICT.
            // This breaks the multiple cascade path.
            builder.HasOne(qi => qi.Quotation)
                   .WithMany(q => q.Items) // Confirmed navigation property from Quotation.cs
                   .HasForeignKey(qi => qi.QuotationId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Explicitly set the relationship to PurchaseRequestItem to CASCADE (default)
            // or confirm if it also needs to be restricted based on other entity constraints.
            // For now, leave it as CASCADE (default) or explicitly set it:
            builder.HasOne(qi => qi.PurchaseRequestItem)
                   .WithMany() // Assuming no ICollection<QuotationItem> on PurchaseRequestItem.cs 
                   .HasForeignKey(qi => qi.PurchaseRequestItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}