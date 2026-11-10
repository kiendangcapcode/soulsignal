using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.Services;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

public class SongsController(ApplicationDbContext db, IWebHostEnvironment environment, VipEntitlements vip,
    AudioProcessingService audio) : Controller
{
    [HttpGet, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var song = await FindSongAsync(id);
        if (song is null) return NotFound();
        if (!await CanAccessAsync(song)) return AccessFailure();
        var hasFlac = song.Status == SongStatus.Approved && audio.FindStoredFile(song.FlacFileName) is not null;
        var canStreamFlac = hasFlac && await vip.HasActiveListenerAsync(User, cancellationToken);
        var isFavorite = User.IsInRole(AppRoles.Listener)
            && User.FindFirstValue(ClaimTypes.NameIdentifier) is string listenerId
            && await db.FavoriteSongs.AnyAsync(x => x.UserId == listenerId && x.SongId == id, cancellationToken);
        return View(new SongDetailsViewModel
        {
            Song = song,
            HasFlac = hasFlac,
            CanStreamFlac = canStreamFlac,
            CanDownloadFlac = canStreamFlac && song.AllowDownload,
            IsFavorite = isFavorite
        });
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Stream(int id)
    {
        var song = await FindSongAsync(id);
        if (song is null) return NotFound();
        if (!await CanAccessAsync(song)) return AccessFailure();
        return AudioFile(song.AudioFileName, "audio/mpeg");
    }

    [AcceptVerbs("GET", "HEAD"), Authorize(Roles = AppRoles.Listener)]
    public Task<IActionResult> StreamFlac(int id, CancellationToken cancellationToken) =>
        VipFlacAsync(id, download: false, cancellationToken);

    [AcceptVerbs("GET", "HEAD"), Authorize(Roles = AppRoles.Listener)]
    public Task<IActionResult> Download(int id, CancellationToken cancellationToken) =>
        VipFlacAsync(id, download: true, cancellationToken);

    private async Task<IActionResult> VipFlacAsync(int id, bool download, CancellationToken cancellationToken)
    {
        if (!await vip.HasActiveListenerAsync(User, cancellationToken)) return Forbid();
        var song = await db.Songs.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == id && x.Status == SongStatus.Approved, cancellationToken);
        if (song is null) return NotFound();
        if (download && !song.AllowDownload) return Forbid();
        return AudioFile(song.FlacFileName, "audio/flac", download ? $"{song.ReferenceCode}.flac" : null);
    }

    // Restricted technical check, not a VIP listener feature.
    [AcceptVerbs("GET", "HEAD"), Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> FlacPreview(int id)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var song = await FindSongAsync(id);
        if (song is null || string.IsNullOrEmpty(song.FlacFileName)) return NotFound();
        if (!await CanAccessAsync(song)) return Forbid();
        return AudioFile(song.FlacFileName, "audio/flac");
    }

    private Task<Song?> FindSongAsync(int id) => db.Songs.AsNoTracking()
        .Include(x => x.Genre).Include(x => x.Distributor).FirstOrDefaultAsync(x => x.Id == id);

    [AcceptVerbs("GET", "HEAD"), Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> HighQualityPreview(int id)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var song = await FindSongAsync(id);
        if (song is null || string.IsNullOrEmpty(song.HighQualityFileName)) return NotFound();
        return AudioFile(song.HighQualityFileName, "audio/mpeg");
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Cover(string id)
    {
        if (string.IsNullOrEmpty(id) || Path.GetFileName(id) != id) return NotFound();
        var song = await db.Songs.AsNoTracking().FirstOrDefaultAsync(x => x.CoverFileName == id);
        if (song is null) return NotFound();
        if (!await CanAccessAsync(song)) return AccessFailure();
        var path = Path.Combine(environment.ContentRootPath, "Media", "Covers", id);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(path, Path.GetExtension(id) == ".png" ? "image/png" : "image/jpeg");
    }

    private async Task<bool> CanAccessAsync(Song song)
    {
        if (song.Status == SongStatus.Approved || User.IsInRole(AppRoles.Admin)) return true;
        if (!User.IsInRole(AppRoles.Distributor) || song.DistributorId is null) return false;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.Users.AnyAsync(x => x.Id == userId && x.DistributorId == song.DistributorId);
    }

    private IActionResult AccessFailure() => User.Identity?.IsAuthenticated == true ? Forbid() : Challenge();

    private IActionResult AudioFile(string? fileName, string contentType, string? downloadName = null)
    {
        var path = audio.FindStoredFile(fileName);
        if (path is null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(path, contentType, downloadName, enableRangeProcessing: true);
    }
}
