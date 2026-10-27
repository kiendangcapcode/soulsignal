using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Models;

namespace SoulSignal.Data;

public static class DemoData
{
    public const string DemoPassword = "123456";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        foreach (var role in new[] { AppRoles.Listener, AppRoles.Distributor, AppRoles.Admin })
            if (!await roles.RoleExistsAsync(role)) Check(await roles.CreateAsync(new IdentityRole(role)));
        await EnsureUserAsync(users, "listener@soulsignal.local", "Người nghe demo", AppRoles.Listener);
        var admin = await EnsureUserAsync(users, "admin@soulsignal.local", "Quản trị Soul Signal", AppRoles.Admin);

        var sources = new[]
        {
            new Distributor { Name = "DistroKite Demo", Code = "DKT-DEMO",
                Description = "Tên hư cấu gợi nhớ DistroKid, chỉ dùng minh họa nguồn cung cấp nhạc cho đồ án." },
            new Distributor { Name = "TuneKore Demo", Code = "TKR-DEMO",
                Description = "Tên hư cấu gợi nhớ TuneCore, không liên kết với doanh nghiệp thật." }
        };
        foreach (var source in sources)
            if (!await db.Distributors.AnyAsync(x => x.Code == source.Code)) db.Distributors.Add(source);
        foreach (var name in new[] { "Electronic", "Ambient", "Lo-fi" })
            if (!await db.Genres.AnyAsync(x => x.Name == name)) db.Genres.Add(new Genre { Name = name });
        await db.SaveChangesAsync();
        var distributors = await db.Distributors.ToDictionaryAsync(x => x.Code, x => x.Id);
        await EnsureDistributorAsync(users, "distrokite@soulsignal.local", "DistroKite Demo", distributors["DKT-DEMO"]);
        await EnsureDistributorAsync(users, "tunekore@soulsignal.local", "TuneKore Demo", distributors["TKR-DEMO"]);
        var genres = await db.Genres.ToDictionaryAsync(x => x.Name, x => x.Id);
        var samples = new[]
        {
            new { Title = "Saigon Dawn", File = "saigon-dawn", Genre = "Lo-fi", Duration = 24,
                Source = "DKT-DEMO", Description = "Một buổi sớm chậm rãi, những hợp âm ấm và nhịp điệu nhẹ. Bản nhạc mẫu được tổng hợp riêng cho Soul Signal.",
                Status = SongStatus.Approved, HasFlac = true },
            new { Title = "Afterglow", File = "afterglow", Genre = "Electronic", Duration = 28,
                Source = "DKT-DEMO", Description = "Dư âm của ánh đèn thành phố, giai điệu điện tử mềm và tiết tấu vừa phải. Nhạc mẫu gốc của dự án.",
                Status = SongStatus.Approved, HasFlac = false },
            new { Title = "Quiet Orbit", File = "quiet-orbit", Genre = "Ambient", Duration = 32,
                Source = "TKR-DEMO", Description = "Một quỹ đạo yên tĩnh với lớp âm thanh rộng, dành cho vài phút nghỉ ngơi. Nhạc mẫu gốc của dự án.",
                Status = SongStatus.Approved, HasFlac = false },
            new { Title = "Quiet Orbit — bản nháp", File = "quiet-orbit", Genre = "Ambient", Duration = 32,
                Source = "TKR-DEMO", Description = "Gói nhạc chờ duyệt mẫu. Chỉ admin được kiểm tra trước khi công khai.",
                Status = SongStatus.Pending, HasFlac = false }
        };
        for (var index = 0; index < samples.Length; index++)
        {
            var sample = samples[index];
            var song = await db.Songs.FirstOrDefaultAsync(x => x.Title == sample.Title && x.AudioFileName == sample.File + ".mp3");
            if (song is null)
            {
                song = new Song
                {
                    Title = sample.Title, Artist = "Soul Signal Studio", Description = sample.Description,
                    DurationSeconds = sample.Duration, CoverPath = $"/images/covers/{sample.File}.svg",
                    AudioFileName = $"{sample.File}.mp3", FlacFileName = sample.HasFlac ? $"{sample.File}.flac" : null,
                    GenreId = genres[sample.Genre], Status = sample.Status,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-4 + index)
                };
                db.Songs.Add(song);
            }
            // Preserve listening data and review decisions while moving old demo ownership to admin.
            song.ImportedById = admin.Id;
            song.DistributorId ??= distributors[sample.Source];
            song.ExternalRecordingCode ??= $"DEMO-REC-{index + 1:000}";
            song.ExternalReleaseCode ??= $"DEMO-REL-{index + 1:000}";
            if (string.IsNullOrEmpty(song.RightsHolder)) song.RightsHolder = "Soul Signal Studio (dữ liệu mẫu)";
            if (string.IsNullOrEmpty(song.RightsEvidence))
                song.RightsEvidence = "Âm thanh tổng hợp bằng scripts/generate-demo-audio.py; ảnh SVG riêng cho đồ án.";
        }
        if (!await db.VipPlans.AnyAsync()) db.VipPlans.Add(new VipPlan
            { Name = "Soul Signal VIP", Price = 29000, DurationDays = 30 });
        await db.SaveChangesAsync();

        // Disable the previous demo Publisher login without destroying its historical account record.
        var legacy = await users.FindByEmailAsync("publisher@soulsignal.local");
        if (legacy is not null)
        {
            Check(await users.RemoveFromRolesAsync(legacy, await users.GetRolesAsync(legacy)));
            Check(await users.SetLockoutEnabledAsync(legacy, true));
            Check(await users.SetLockoutEndDateAsync(legacy, DateTimeOffset.MaxValue));
            Check(await users.UpdateSecurityStampAsync(legacy));
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> users,
        string email, string displayName, string role)
    {
        var user = await users.FindByEmailAsync(email);
        var userName = email.Split('@')[0];
        if (user is null)
        {
            user = new ApplicationUser { UserName = userName, Email = email, DisplayName = displayName, EmailConfirmed = true };
            Check(await users.CreateAsync(user, DemoPassword));
        }
        else
        {
            // Development demo accounts keep their IDs, source links and song history.
            if (user.UserName != userName)
            {
                Check(await users.SetUserNameAsync(user, userName));
                Check(await users.UpdateSecurityStampAsync(user));
            }
            if (!await users.CheckPasswordAsync(user, DemoPassword))
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                Check(await users.ResetPasswordAsync(user, token, DemoPassword));
            }
            if (user.AccessFailedCount != 0) Check(await users.ResetAccessFailedCountAsync(user));
            if (user.LockoutEnd.HasValue) Check(await users.SetLockoutEndDateAsync(user, null));
        }
        if (!await users.IsInRoleAsync(user, role)) Check(await users.AddToRoleAsync(user, role));
        return user;
    }

    private static async Task EnsureDistributorAsync(UserManager<ApplicationUser> users,
        string email, string name, int distributorId)
    {
        var user = await EnsureUserAsync(users, email, name, AppRoles.Distributor);
        if (user.DistributorId == distributorId) return;
        user.DistributorId = distributorId;
        Check(await users.UpdateAsync(user));
        Check(await users.UpdateSecurityStampAsync(user));
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
