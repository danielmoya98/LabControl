using Microsoft.AspNetCore.Mvc;
using LabControl.Application.Features.Onboarding.Commands.CompleteOnboarding;
using LabControl.Application.Features.Onboarding.Queries.GetOnboardingStatus;

namespace LabControl.Api.Controllers;

[Route("api/[controller]")]
public class OnboardingController : ApiControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOnboardingStatusQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
