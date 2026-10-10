using System.Security.Claims;
using BusinessLogicLayer.Contracts.Regulations;
using WebApi.Auth;

namespace WebApi.Services;

public static class RegulationSubjectResolver
{
    public static RegulationSubject Resolve(ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var accountIdClaim = role == AuthRoles.Manager ? "manager_id" : "employee_id";
        if (!int.TryParse(user.FindFirstValue(accountIdClaim), out var accountId) || accountId <= 0)
        {
            throw new BadHttpRequestException("The current account session is invalid.");
        }

        return new RegulationSubject
        {
            Role = role,
            AccountId = accountId,
            Username = user.Identity?.Name ?? string.Empty,
            DisplayName = user.FindFirstValue("display_name") ?? user.Identity?.Name ?? string.Empty,
        };
    }
}
