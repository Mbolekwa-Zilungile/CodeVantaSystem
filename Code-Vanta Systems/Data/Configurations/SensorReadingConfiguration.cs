using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Code_Vanta_Systems.Data.Configurations
{
    public class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
    {
        public void Configure(EntityTypeBuilder<SensorReading> builder)
        {
            builder.Property(sr => sr.TemperatureCelsius)
                   .HasColumnType("decimal(4,2)");

            builder.Property(sr => sr.HumidityPercent)
                   .HasColumnType("decimal(4,2)");

            // CHANGE: Fridge → FridgeUnit
            builder.HasOne(sr => sr.FridgeUnit)
                   .WithMany(f => f.SensorReadings)
                   .HasForeignKey(sr => sr.FridgeUnitId)  // Updated property name
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}