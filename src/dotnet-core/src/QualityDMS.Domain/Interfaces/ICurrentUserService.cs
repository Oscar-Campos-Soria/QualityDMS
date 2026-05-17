namespace QualityDMS.Domain.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string UserName { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
}
