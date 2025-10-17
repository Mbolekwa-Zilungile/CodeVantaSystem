using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(c => c.Address)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.HasData(new Company
            {
                Id = 1,
                Name = "Codevanta Systems Ltd.",
                Email = "codevanta@gmail.com",
                Phone = "+27 83 193 9887",
                Address = "123 Western Way, Port Elizabeth, South Africa",
                RegistrationNumber = "1234567890",
                VatNumber = "VAT123456"
            });
        }
    }
}
