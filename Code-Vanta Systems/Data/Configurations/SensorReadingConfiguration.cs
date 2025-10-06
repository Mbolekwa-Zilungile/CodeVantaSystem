using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
    {
        public void Configure(EntityTypeBuilder<SensorReading> builder)
        {
            // Set precision for temperature 
            builder.Property(sr => sr.TemperatureCelsius)
                   .HasColumnType("decimal(4,2)");

            // Fixes HumidityPercent precision warning
            builder.Property(sr => sr.HumidityPercent)
                   .HasColumnType("decimal(4,2)");

            // Relationship: SensorReading (Many) → Fridge (One)
            builder.HasOne(sr => sr.Fridge)
                   .WithMany(f => f.SensorReadings)
                   .HasForeignKey(sr => sr.FridgeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}