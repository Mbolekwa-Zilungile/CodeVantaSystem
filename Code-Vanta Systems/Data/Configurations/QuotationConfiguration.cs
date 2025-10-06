using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            // FIX: Explicitly configure the Quotation -> PurchaseRequest relationship
            // This resolves the 'shadow property' warning.
            builder.HasOne(q => q.PurchaseRequest)
                   .WithMany(pr => pr.Quotations) // <--- ASSUMED NAVIGATION PROPERTY NAME
                   .HasForeignKey(q => q.PurchaseRequestId)
                   .OnDelete(DeleteBehavior.Cascade); // Use Cascade as it is the main link

            // Supplier → Quotation (Existing)
            builder.HasOne(q => q.Supplier)
                   .WithMany(s => s.Quotations)
                   .HasForeignKey(q => q.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // RequestQuotation → Quotation (Existing)
            builder.HasOne(q => q.RequestQuotation)
                   .WithMany(rq => rq.Quotations)
                   .HasForeignKey(q => q.RequestQuotationId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}