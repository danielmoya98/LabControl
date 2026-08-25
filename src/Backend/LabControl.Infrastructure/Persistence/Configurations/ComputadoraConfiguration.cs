using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class ComputadoraConfiguration : IEntityTypeConfiguration<Computadora>
{
    public void Configure(EntityTypeBuilder<Computadora> builder)
    {
        builder.ToTable("Computadoras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Hostname)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(c => c.Hostname).IsUnique();

        builder.Property(c => c.MacAddress)
            .HasMaxLength(17)
            .IsRequired();

        builder.HasIndex(c => c.MacAddress).IsUnique();

        builder.Property(c => c.IpActual)
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(c => c.EstadoActual)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(c => c.SesionesUso)
            .WithOne(s => s.Computadora)
            .HasForeignKey(s => s.ComputadoraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
