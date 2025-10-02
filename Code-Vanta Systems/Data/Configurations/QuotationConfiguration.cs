using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
    {
        public void Configure(EntityTypeBuilder<Quotation> builder)
        {
            // Supplier → Quotation
            builder.HasOne(q => q.Supplier)
                   .WithMany(s => s.Quotations)
                   .HasForeignKey(q => q.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // RequestQuotation → Quotation
            builder.HasOne(q => q.RequestQuotation)
                   .WithMany(rq => rq.Quotations)
                   .HasForeignKey(q => q.RequestQuotationId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
