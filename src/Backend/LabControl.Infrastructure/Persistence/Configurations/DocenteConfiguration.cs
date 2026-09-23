using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class DocenteConfiguration : IEntityTypeConfiguration<Docente>
{
    public void Configure(EntityTypeBuilder<Docente> builder)
    {
        builder.ToTable("Docentes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Nombres)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(d => d.Apellidos)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(d => d.EmailInstitucional)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(d => d.EmailInstitucional)
            .IsUnique();

        builder.Property(d => d.TelefonoContacto)
            .HasMaxLength(20);

        builder.Property(d => d.Activo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(d => d.BloquesHorarios)
            .WithOne(b => b.Docente)
            .HasForeignKey(b => b.DocenteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
