using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class SedeConfiguration : IEntityTypeConfiguration<Sede>
{
    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        builder.ToTable("Sedes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Codigo)
            .HasMaxLength(15)
            .IsRequired();

        builder.HasIndex(s => s.Codigo)
            .IsUnique();

        builder.Property(s => s.Ciudad)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(s => s.Direccion)
            .HasMaxLength(150);

        builder.Property(s => s.SegmentoRed)
            .HasMaxLength(50);

        builder.Property(s => s.OnboardingCompletado)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.FechaRegistroUtc)
            .IsRequired();

        builder.HasMany(s => s.Bloques)
            .WithOne(b => b.Sede)
            .HasForeignKey(b => b.SedeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
