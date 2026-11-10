using SoulSignal.Models;

namespace SoulSignal.ViewModels;

public class VipIndexViewModel
{
    public VipPlan? Plan { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public DateTimeOffset? LastExpiredAt { get; init; }
    public Guid RequestId { get; init; }
    public bool CheckoutEnabled { get; init; }
    public IReadOnlyList<VipOrderViewModel> Orders { get; init; } = [];
}

public record VipOrderViewModel(int Id, string PlanName, decimal Amount, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, DemoOrderStatus Status)
{
    public string Code => $"SSVIP{Id:000000}";
}
