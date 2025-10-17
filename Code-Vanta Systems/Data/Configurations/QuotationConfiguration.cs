using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
            public void Configure(EntityTypeBuilder<Quotation> builder)
            {
                // 1️⃣ Quotation → PurchaseRequest
                builder.HasOne(q => q.PurchaseRequest)
                       .WithMany(pr => pr.Quotations) // Make sure PurchaseRequest has ICollection<Quotation> Quotation navigation
                       .HasForeignKey(q => q.PurchaseRequestId)
                       .OnDelete(DeleteBehavior.Cascade); // Primary relationship, cascade ok

                // 2️⃣ Quotation → Supplier
                builder.HasOne(q => q.Supplier)
                       .WithMany(s => s.Quotations)
                       .HasForeignKey(q => q.SupplierId)
                       .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete to avoid multiple cascade paths

                // 3️⃣ Quotation → RequestQuotation
                builder.HasOne(q => q.RequestQuotation)
                       .WithMany(rq => rq.Quotations)
                       .HasForeignKey(q => q.RequestQuotationId)
                       .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths
            }
    }
}