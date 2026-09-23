using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class BloqueConfiguration : IEntityTypeConfiguration<Bloque>
{
    public void Configure(EntityTypeBuilder<Bloque> builder)
    {
        builder.ToTable("Bloques");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Nombre)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(b => b.Codigo)
            .HasMaxLength(15)
            .IsRequired();

        builder.HasIndex(b => b.Codigo)
            .IsUnique();

        builder.Property(b => b.TienePisos)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(b => b.TotalPisos);

        builder.Property(b => b.Descripcion)
            .HasMaxLength(150);

        builder.Property(b => b.Activo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(b => b.Aulas)
            .WithOne(a => a.Bloque)
            .HasForeignKey(a => a.BloqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
