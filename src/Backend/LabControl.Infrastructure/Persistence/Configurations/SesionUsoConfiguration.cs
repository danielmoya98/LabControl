using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LabControl.Domain.Entities;

namespace LabControl.Infrastructure.Persistence.Configurations;

public class SesionUsoConfiguration : IEntityTypeConfiguration<SesionUso>
{
    public void Configure(EntityTypeBuilder<SesionUso> builder)
    {
        builder.ToTable("SesionesUso");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmailEstudiante)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(s => s.EmailEstudiante);
        builder.HasIndex(s => s.FechaHoraInicio);

        builder.Property(s => s.TipoCierre)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.SyncStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}
