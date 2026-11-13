using System.Security.Claims;
using DocReview.DataModels;

namespace DocReview.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        return claim is null ? null : Guid.Parse(claim.Value);
    }
    
    public static string? GetUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static string? GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value;
    }

    public static UserRole? GetUserRole(this ClaimsPrincipal principal)
    {
        var role = GetRole(principal);
        if (role is null)
        {
            return null;
        }

        return role switch
        {
            "Author" => UserRole.Author,
            "Reviewer" => UserRole.Reviewer,
            "Admin" => UserRole.Admin,
            _ => null
        };
    }
}