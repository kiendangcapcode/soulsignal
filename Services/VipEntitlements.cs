using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;

namespace SoulSignal.Services;

public class VipEntitlements(ApplicationDbContext db)
{
    public async Task<bool> HasActiveListenerAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return user.IsInRole(AppRoles.Listener) && userId is not null
            && await GetActiveEndAsync(userId, cancellationToken) is not null;
    }

    public Task<DateTimeOffset?> GetActiveEndAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return db.VipSubscriptions
            .Where(x => x.UserId == userId && x.StartsAt <= now && x.EndsAt > now)
            .MaxAsync(x => (DateTimeOffset?)x.EndsAt, cancellationToken);
    }
}
