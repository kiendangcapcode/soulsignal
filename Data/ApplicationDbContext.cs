using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Models;

namespace SoulSignal.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<VipPlan> VipPlans => Set<VipPlan>();
    public DbSet<VipSubscription> VipSubscriptions => Set<VipSubscription>();
    public DbSet<DemoOrder> DemoOrders => Set<DemoOrder>();
    public DbSet<Distributor> Distributors => Set<Distributor>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().HasOne(x => x.Distributor).WithMany()
            .HasForeignKey(x => x.DistributorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Genre>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<Song>().Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Entity<Song>().HasIndex(x => new { x.Status, x.CreatedAt });
        builder.Entity<Song>().HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Song>().HasOne(x => x.ImportedBy).WithMany().HasForeignKey(x => x.ImportedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Song>().HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Distributor>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Song>().HasOne(x => x.Distributor).WithMany().HasForeignKey(x => x.DistributorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Song>().HasIndex(x => new { x.DistributorId, x.ExternalRecordingCode }).IsUnique()
            .HasFilter("[DistributorId] IS NOT NULL AND [ExternalRecordingCode] IS NOT NULL");
        builder.Entity<VipPlan>().Property(x => x.Price).HasPrecision(12, 2);
        builder.Entity<VipSubscription>().HasIndex(x => new { x.UserId, x.EndsAt });
        builder.Entity<VipSubscription>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.Entity<VipSubscription>().HasOne(x => x.VipPlan).WithMany().HasForeignKey(x => x.VipPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<DemoOrder>().Property(x => x.Amount).HasPrecision(12, 2);
        builder.Entity<DemoOrder>().Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Entity<DemoOrder>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.Entity<DemoOrder>().HasOne(x => x.VipPlan).WithMany().HasForeignKey(x => x.VipPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
