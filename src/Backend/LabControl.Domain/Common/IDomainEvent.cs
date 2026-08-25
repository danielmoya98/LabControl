namespace LabControl.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
