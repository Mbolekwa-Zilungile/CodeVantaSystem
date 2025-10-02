using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class RequestQuotationConfiguration : IEntityTypeConfiguration<RequestQuotation>
    {
        public void Configure(EntityTypeBuilder<RequestQuotation> builder)
        {
            builder.HasKey(rq => rq.RequestQuotationId);

            builder.Property(rq => rq.Notes)
                   .HasMaxLength(500);

            // Relationship: RequestQuotation -> Supplier
            builder.HasOne(rq => rq.Supplier)
                   .WithMany(s => s.RequestQuotations)
                   .HasForeignKey(rq => rq.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict); // ✅ Fix multiple cascade paths

            // Relationship: RequestQuotation -> ApplicationUser (RequestedBy)
            builder.HasOne(rq => rq.RequestedBy)
                   .WithMany()
                   .HasForeignKey(rq => rq.RequestedByUserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
