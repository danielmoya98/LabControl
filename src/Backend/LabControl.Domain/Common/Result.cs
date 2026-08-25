namespace LabControl.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("No se puede crear un Result de éxito con un Error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("No se puede crear un Result de falla sin especificar un Error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}
