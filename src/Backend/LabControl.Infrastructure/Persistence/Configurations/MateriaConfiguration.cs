using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class MateriaConfiguration : IEntityTypeConfiguration<Materia>
{
    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("Materias");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Sigla)
            .HasMaxLength(15)
            .IsRequired();

        builder.HasIndex(m => m.Sigla)
            .IsUnique();

        builder.Property(m => m.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Carrera)
            .HasMaxLength(80);

        builder.Property(m => m.Activo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(m => m.BloquesHorarios)
            .WithOne(b => b.Materia)
            .HasForeignKey(b => b.MateriaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
