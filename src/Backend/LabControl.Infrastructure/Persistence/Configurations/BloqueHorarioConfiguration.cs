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

        builder.Property(b => b.Descripcion)
            .HasMaxLength(100);
    }
}
