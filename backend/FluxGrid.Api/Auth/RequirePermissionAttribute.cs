using Microsoft.AspNetCore.Authorization;

namespace FluxGrid.Api.Auth;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}
