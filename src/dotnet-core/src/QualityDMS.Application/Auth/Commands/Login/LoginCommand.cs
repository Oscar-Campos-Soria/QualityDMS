using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResult>>;

public record LoginResult(string Token, string UserName, string Email, IEnumerable<string> Roles, DateTime Expires);
