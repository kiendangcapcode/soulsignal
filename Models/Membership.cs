using System.ComponentModel.DataAnnotations;

namespace SoulSignal.Models;

// Schema prepared for week 3; week 1 does not sell or activate subscriptions.
public class VipPlan
{
    public int Id { get; set; }
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    [Range(1, 3660)]
    public int DurationDays { get; set; }
}

public class VipSubscription
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int VipPlanId { get; set; }
    public VipPlan VipPlan { get; set; } = null!;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
}

public enum DemoOrderStatus { Pending, Succeeded, Failed }

public class DemoOrder
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int VipPlanId { get; set; }
    public VipPlan VipPlan { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DemoOrderStatus Status { get; set; } = DemoOrderStatus.Pending;
}
