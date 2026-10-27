using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SoulSignal.Models;

namespace SoulSignal.Services;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(users, roles, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("DisplayName", user.DisplayName));
        return identity;
    }
}
