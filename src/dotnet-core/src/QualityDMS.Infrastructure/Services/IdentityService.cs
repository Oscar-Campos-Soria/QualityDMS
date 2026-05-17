using Microsoft.AspNetCore.Identity;
using QualityDMS.Application.Auth.Commands.Login;
using QualityDMS.Infrastructure.Identity;

namespace QualityDMS.Infrastructure.Services;

public class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<(bool Success, string UserId, string UserName, IEnumerable<string> Roles)>
        ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return (false, string.Empty, string.Empty, []);

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return (false, string.Empty, string.Empty, []);

        var roles = await userManager.GetRolesAsync(user);
        return (true, user.Id, user.FullName, roles);
    }
}
