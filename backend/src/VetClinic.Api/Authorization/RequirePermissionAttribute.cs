using Microsoft.AspNetCore.Authorization;
using VetClinic.Domain;

namespace VetClinic.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission) : base(Permissions.PolicyName(permission))
    {
    }
}
