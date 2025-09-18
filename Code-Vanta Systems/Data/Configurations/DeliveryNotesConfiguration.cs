using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class DeliveryNoteConfiguration : IEntityTypeConfiguration<DeliveryNote>
    {
        public void Configure(EntityTypeBuilder<DeliveryNote> builder)
        {
            // Primary Key
            builder.HasKey(d => d.Id);

            // Properties
            builder.Property(d => d.DeliveryDate)
                   .IsRequired();

            builder.Property(d => d.Notes)
                   .HasMaxLength(500);

            // Relationships

            // Supplier → One supplier can have many delivery notes
            builder.HasOne(d => d.Supplier)
                   .WithMany(s => s.DeliveryNotes)
                   .HasForeignKey(d => d.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrder → One purchase order can generate many delivery notes
            builder.HasOne(d => d.PurchaseOrder)
                   .WithMany(p => p.DeliveryNotes)
                   .HasForeignKey(d => d.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ReceivedBy → (ApplicationUser), optional or required depending on model
            builder.HasOne(d => d.ReceivedBy)
                   .WithMany()
                   .HasForeignKey(d => d.ReceivedById)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.HasIndex(d => d.SupplierId);
            builder.HasIndex(d => d.PurchaseOrderId);
            builder.HasIndex(d => d.ReceivedById);
        }
    }
}
