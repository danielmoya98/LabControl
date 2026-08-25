using LabControl.Application.Common.Interfaces;

namespace LabControl.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
