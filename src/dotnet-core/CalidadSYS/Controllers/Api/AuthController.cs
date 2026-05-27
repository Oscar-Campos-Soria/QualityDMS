using MediatR;
using Microsoft.AspNetCore.Mvc;
using QualityDMS.Application.Auth.Commands.Login;

namespace CalidadSYS.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new LoginCommand(req.Email, req.Password), ct);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(result.Error);
    }
}

public record LoginRequest(string Email, string Password);
