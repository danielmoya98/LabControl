using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class AulaConfiguration : IEntityTypeConfiguration<Aula>
{
    public void Configure(EntityTypeBuilder<Aula> builder)
    {
        builder.ToTable("Aulas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nombre)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Pabellon)
            .HasMaxLength(50);

        builder.HasMany(a => a.Computadoras)
            .WithOne(c => c.Aula)
            .HasForeignKey(c => c.AulaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.BloquesHorarios)
            .WithOne(b => b.Aula)
            .HasForeignKey(b => b.AulaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
