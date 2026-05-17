using MediatR;
using QualityDMS.Domain.Common;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Application.Auth.Commands.Login;

public interface IIdentityService
{
    Task<(bool Success, string UserId, string UserName, IEnumerable<string> Roles)> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default);
}

public interface ITokenService
{
    (string Token, DateTime Expires) GenerateToken(string userId, string userName, string email, IEnumerable<string> roles);
}

public class LoginCommandHandler(IIdentityService identityService, ITokenService tokenService)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var (success, userId, userName, roles) = await identityService.ValidateCredentialsAsync(cmd.Email, cmd.Password, ct);

        if (!success)
            return Result.Failure<LoginResult>("Credenciales inválidas.");

        var (token, expires) = tokenService.GenerateToken(userId, userName, cmd.Email, roles);

        return Result.Success(new LoginResult(token, userName, cmd.Email, roles, expires));
    }
}
