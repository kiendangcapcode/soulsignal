using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.Services;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

public class HomeController(ApplicationDbContext db, VipEntitlements vip, AudioProcessingService audio) : Controller
{
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index(string? q, int? genreId, string? artist, CancellationToken cancellationToken)
    {
        q = q?.Trim();
        if (q?.Length > 120) q = q[..120];
        artist = artist?.Trim();
        if (artist?.Length > 120) return BadRequest();

        var songs = db.Songs.AsNoTracking().Include(x => x.Genre).Include(x => x.Distributor)
            .Where(x => x.Status == SongStatus.Approved);
        if (!string.IsNullOrEmpty(artist)) songs = songs.Where(x => x.Artist == artist);
        if (!string.IsNullOrWhiteSpace(q))
            songs = songs.Where(x => x.Title.Contains(q) || x.Artist.Contains(q));
        if (genreId.HasValue) songs = songs.Where(x => x.GenreId == genreId);
        var orderedSongs = songs.OrderBy(x => x.Id);
        var results = await (string.IsNullOrEmpty(artist) ? orderedSongs.Take(100) : orderedSongs)
            .ToListAsync(cancellationToken);
        var flacSongIds = results.Where(x => audio.FindStoredFile(x.FlacFileName) is not null)
            .Select(x => x.Id).ToHashSet();
        var hasVip = flacSongIds.Count > 0
            && await vip.HasActiveListenerAsync(User, cancellationToken);
        HashSet<int> favoriteSongIds = [];
        if (results.Count > 0 && User.IsInRole(AppRoles.Listener) &&
            User.FindFirstValue(ClaimTypes.NameIdentifier) is string listenerId)
        {
            var visibleIds = results.Select(song => song.Id).ToArray();
            favoriteSongIds = (await db.FavoriteSongs.AsNoTracking()
                .Where(favorite => favorite.UserId == listenerId && visibleIds.Contains(favorite.SongId))
                .Select(favorite => favorite.SongId).ToListAsync(cancellationToken)).ToHashSet();
        }
        return View(new CatalogViewModel
        {
            Songs = results,
            FlacSongIds = flacSongIds,
            FavoriteSongIds = favoriteSongIds,
            HasActiveVip = hasVip,
            Genres = await db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken),
            Query = q, GenreId = genreId, Artist = artist
        });
    }

    [HttpGet]
    public IActionResult Scope() => View();

    [HttpGet]
    public IActionResult Policy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
