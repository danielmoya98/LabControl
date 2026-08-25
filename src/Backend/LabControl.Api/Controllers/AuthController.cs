using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Auth.Commands.LoginAdmin;
using LabControl.Application.Features.Auth.Commands.LoginStudent;

namespace LabControl.Api.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("login-student")]
    public async Task<IActionResult> LoginStudent([FromBody] LoginStudentCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("login-admin")]
    public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
