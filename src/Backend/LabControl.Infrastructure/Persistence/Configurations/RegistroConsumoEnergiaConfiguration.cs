using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class RegistroConsumoEnergiaConfiguration : IEntityTypeConfiguration<RegistroConsumoEnergia>
{
    public void Configure(EntityTypeBuilder<RegistroConsumoEnergia> builder)
    {
        builder.ToTable("RegistrosConsumoEnergia");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UltimoEstudianteEmail)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(r => r.UltimoEstudianteNombre)
            .HasMaxLength(150);

        builder.Property(r => r.MotivoInfraccion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.HorasInactivaEncendida)
            .IsRequired();

        builder.HasOne(r => r.Computadora)
            .WithMany()
            .HasForeignKey(r => r.ComputadoraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Aula)
            .WithMany()
            .HasForeignKey(r => r.AulaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.SesionUso)
            .WithMany()
            .HasForeignKey(r => r.SesionUsoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.FechaDeteccionUtc);
        builder.HasIndex(r => r.UltimoEstudianteEmail);
    }
}
