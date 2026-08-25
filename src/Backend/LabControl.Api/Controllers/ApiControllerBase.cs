using MediatR;
using Microsoft.AspNetCore.Mvc;
using LabControl.Domain.Common;

namespace LabControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return MapErrorToResponse(result.Error);
    }

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapErrorToResponse(result.Error);
    }

    private IActionResult MapErrorToResponse(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new ProblemDetails
            {
                Title = "Recurso no encontrado",
                Detail = error.Message,
                Status = StatusCodes.Status404NotFound
            }),
            ErrorType.Validation => BadRequest(new ProblemDetails
            {
                Title = "Error de validación",
                Detail = error.Message,
                Status = StatusCodes.Status400BadRequest
            }),
            ErrorType.Unauthorized => Unauthorized(new ProblemDetails
            {
                Title = "No autorizado",
                Detail = error.Message,
                Status = StatusCodes.Status401Unauthorized
            }),
            ErrorType.Conflict => Conflict(new ProblemDetails
            {
                Title = "Conflicto",
                Detail = error.Message,
                Status = StatusCodes.Status409Conflict
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Error interno del servidor",
                Detail = error.Message,
                Status = StatusCodes.Status500InternalServerError
            })
        };
    }
}
