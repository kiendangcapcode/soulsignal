using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

public class SongsController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var song = await FindSongAsync(id);
        if (song is null) return NotFound();
        if (!await CanAccessAsync(song)) return AccessFailure();
        return View(new SongDetailsViewModel { Song = song });
    }

    [AcceptVerbs("GET", "HEAD")]
    public async Task<IActionResult> Stream(int id)
    {
        var song = await FindSongAsync(id);
        if (song is null) return NotFound();
        if (!await CanAccessAsync(song)) return AccessFailure();
        return AudioFile(song.AudioFileName, "audio/mpeg");
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

    private IActionResult AudioFile(string fileName, string contentType)
    {
        if (Path.GetFileName(fileName) != fileName) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "Media", "Audio", fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }
}
