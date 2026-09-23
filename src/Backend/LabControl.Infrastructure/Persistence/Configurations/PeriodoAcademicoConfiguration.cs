using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class PeriodoAcademicoConfiguration : IEntityTypeConfiguration<PeriodoAcademico>
{
    public void Configure(EntityTypeBuilder<PeriodoAcademico> builder)
    {
        builder.ToTable("PeriodosAcademicos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.Nombre)
            .IsUnique();

        builder.Property(p => p.FechaInicio)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.FechaFin)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.EsActual)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(p => p.EsActual);

        builder.Property(p => p.Activo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(p => p.FechaRegistroUtc)
            .IsRequired();

        builder.HasMany(p => p.BloquesHorarios)
            .WithOne(b => b.PeriodoAcademico)
            .HasForeignKey(b => b.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
