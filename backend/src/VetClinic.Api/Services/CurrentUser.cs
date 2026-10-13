using System.Security.Claims;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? UserId
    {
        get
        {
            var raw = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
