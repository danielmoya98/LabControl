namespace LabControl.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacionUtc { get; set; }
}
