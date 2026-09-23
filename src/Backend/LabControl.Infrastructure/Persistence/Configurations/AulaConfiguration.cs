using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;
using LabControl.Domain.Enums;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class AulaConfiguration : IEntityTypeConfiguration<Aula>
{
    public void Configure(EntityTypeBuilder<Aula> builder)
    {
        builder.ToTable("Aulas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nombre)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(a => a.Piso)
            .HasMaxLength(30);

        builder.Property(a => a.Pabellon)
            .HasMaxLength(50);

        builder.Property(a => a.MinutosInactividadMaximo)
            .HasDefaultValue(15)
            .IsRequired();

        builder.Property(a => a.AccionInactividad)
            .HasConversion<int>()
            .HasDefaultValue(TipoAccionInactividad.ApagarEquipo)
            .IsRequired();

        builder.HasOne(a => a.Bloque)
            .WithMany(b => b.Aulas)
            .HasForeignKey(a => a.BloqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Computadoras)
            .WithOne(c => c.Aula)
            .HasForeignKey(c => c.AulaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.BloquesHorarios)
            .WithOne(b => b.Aula)
            .HasForeignKey(b => b.AulaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
