using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.Services;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

[Authorize(Roles = AppRoles.Listener)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class VipController(ApplicationDbContext db, VipEntitlements entitlements, IWebHostEnvironment environment) : Controller
{
    private static readonly TimeSpan OrderLifetime = TimeSpan.FromMinutes(15);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var now = DateTimeOffset.UtcNow;
        var endsAt = await entitlements.GetActiveEndAsync(userId, cancellationToken);
        var checkoutEnabled = environment.IsDevelopment();
        var orders = checkoutEnabled
            ? await db.DemoOrders.AsNoTracking().Include(x => x.VipPlan)
                .Where(x => x.UserId == userId).OrderByDescending(x => x.Id).Take(5).ToListAsync(cancellationToken)
            : [];
        return View(new VipIndexViewModel
        {
            Plan = await GetPlanAsync(cancellationToken),
            EndsAt = endsAt,
            LastExpiredAt = endsAt is null
                ? await db.VipSubscriptions.Where(x => x.UserId == userId && x.EndsAt <= now)
                    .MaxAsync(x => (DateTimeOffset?)x.EndsAt, cancellationToken)
                : null,
            RequestId = Guid.NewGuid(),
            CheckoutEnabled = checkoutEnabled,
            Orders = orders.Select(x => ToViewModel(x, now)).ToList()
        });
    }

    [HttpPost]
    public Task<IActionResult> Start(Guid requestId, int planId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || requestId == Guid.Empty || planId <= 0)
            return Task.FromResult(ShowMessage("Yêu cầu mua gói không hợp lệ. Vui lòng thử lại."));
        return WithOrderLockAsync(async (userId, token) =>
        {
            var previous = await db.DemoOrders
                .Where(x => x.UserId == userId && x.RequestId == requestId)
                .Select(x => (int?)x.Id).SingleOrDefaultAsync(token);
            if (previous is not null)
                return RedirectToAction(nameof(Checkout), new { id = previous.Value });

            if (await entitlements.GetActiveEndAsync(userId, token) is not null)
                return ShowMessage("Gói VIP của bạn vẫn còn hiệu lực.", success: true);
            var plan = await GetPlanAsync(token);
            if (plan is null || plan.Id != planId)
                return ShowMessage("Gói VIP đã thay đổi. Vui lòng kiểm tra và thử lại.");

            var now = DateTimeOffset.UtcNow;
            var cutoff = now - OrderLifetime;
            var pending = await db.DemoOrders
                .Where(x => x.UserId == userId && x.Status == DemoOrderStatus.Pending && x.CreatedAt > cutoff)
                .OrderByDescending(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync(token);
            if (pending is not null)
                return RedirectToAction(nameof(Checkout), new { id = pending.Value });

            var order = new DemoOrder
            {
                RequestId = requestId,
                UserId = userId,
                VipPlanId = plan.Id,
                Amount = plan.Price,
                CreatedAt = now
            };
            db.DemoOrders.Add(order);
            await db.SaveChangesAsync(token);
            return RedirectToAction(nameof(Checkout), new { id = order.Id });
        }, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int id, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var order = await db.DemoOrders.AsNoTracking().Include(x => x.VipPlan)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        return order is null ? NotFound() : View(ToViewModel(order, DateTimeOffset.UtcNow));
    }

    [HttpPost]
    public Task<IActionResult> Simulate(int id, bool? succeeded, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || succeeded is null)
            return Task.FromResult<IActionResult>(BadRequest());

        return WithOrderLockAsync(async (userId, token) =>
        {
            var order = await db.DemoOrders.Include(x => x.VipPlan)
                .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, token);
            if (order is null) return NotFound();

            var now = DateTimeOffset.UtcNow;
            if (EffectiveStatus(order, now) != DemoOrderStatus.Pending)
                return RedirectToAction(nameof(Checkout), new { id });
            if (succeeded.Value && await entitlements.GetActiveEndAsync(userId, token) is not null)
                return ShowMessage("Gói VIP của bạn vẫn còn hiệu lực.", success: true);

            order.Status = succeeded.Value ? DemoOrderStatus.Succeeded : DemoOrderStatus.Failed;
            if (succeeded.Value)
                db.VipSubscriptions.Add(new VipSubscription
                {
                    UserId = userId,
                    VipPlanId = order.VipPlanId,
                    StartsAt = now,
                    EndsAt = now.AddDays(order.VipPlan.DurationDays)
                });

            await db.SaveChangesAsync(token);
            return RedirectToAction(nameof(Checkout), new { id });
        }, cancellationToken);
    }

    private async Task<IActionResult> WithOrderLockAsync(
        Func<string, CancellationToken, Task<IActionResult>> operation, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        return await db.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async token =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(token);

            // Serialize order creation and confirmation across tabs for the same account.
            var lockedUserId = await db.Users
                .FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}")
                .Select(x => x.Id).SingleOrDefaultAsync(token);
            if (lockedUserId is null) return Forbid();

            var result = await operation(userId, token);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }

    private static DemoOrderStatus EffectiveStatus(DemoOrder order, DateTimeOffset now) =>
        order.Status == DemoOrderStatus.Pending && order.CreatedAt + OrderLifetime <= now
            ? DemoOrderStatus.Expired : order.Status;

    private static VipOrderViewModel ToViewModel(DemoOrder order, DateTimeOffset now) =>
        new(order.Id, order.VipPlan.Name, order.Amount, order.CreatedAt, order.CreatedAt + OrderLifetime,
            EffectiveStatus(order, now));

    private Task<VipPlan?> GetPlanAsync(CancellationToken cancellationToken) =>
        db.VipPlans.AsNoTracking().Where(x => x.Price > 0 && x.DurationDays >= 1 && x.DurationDays <= 3660)
            .OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

    private IActionResult ShowMessage(string message, bool success = false)
    {
        TempData[success ? "VipSuccess" : "VipError"] = message;
        return RedirectToAction(nameof(Index));
    }
}
