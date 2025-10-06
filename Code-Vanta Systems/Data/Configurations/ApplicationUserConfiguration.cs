using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Existing Employee Link
            builder.HasOne(a => a.EmployeeProfile)
                   .WithOne(e => e.User)
                   .HasForeignKey<Employee>(e => e.ApplicationUserId);

            // 🛠️ ADDED: Customer Link
            builder.HasOne(a => a.CustomerProfile)
                   .WithOne(c => c.User)
                   .HasForeignKey<Customer>(c => c.UserId) // Using UserId property from Customer.cs
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}