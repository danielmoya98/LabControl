using LabControl.Domain.Common;

namespace LabControl.Domain.Exceptions;

public class BusinessRuleValidationException : DomainException
{
    public Error Error { get; }

    public BusinessRuleValidationException(Error error) : base(error.Message)
    {
        Error = error;
    }
}
