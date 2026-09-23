using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class BloqueHorarioConfiguration : IEntityTypeConfiguration<BloqueHorario>
{
    public void Configure(EntityTypeBuilder<BloqueHorario> builder)
    {
        builder.ToTable("BloquesHorarios");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.DiaSemana)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.HoraInicio)
            .IsRequired();

        builder.Property(b => b.HoraFin)
            .IsRequired();

        builder.Property(b => b.EsRecreo)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(b => b.EsUsoLibre)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(b => b.GrupoParalelo)
            .HasMaxLength(20);

        builder.Property(b => b.Descripcion)
            .HasMaxLength(150);

        builder.Property(b => b.DocenteNombreManual)
            .HasMaxLength(100);

        builder.Property(b => b.DocenteEmailManual)
            .HasMaxLength(150);

        builder.Property(b => b.MateriaNombreManual)
            .HasMaxLength(100);

        builder.HasOne(b => b.Materia)
            .WithMany(m => m.BloquesHorarios)
            .HasForeignKey(b => b.MateriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.Docente)
            .WithMany(d => d.BloquesHorarios)
            .HasForeignKey(b => b.DocenteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.PeriodoAcademico)
            .WithMany(p => p.BloquesHorarios)
            .HasForeignKey(b => b.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(b => b.PeriodoAcademicoId);
    }
}
