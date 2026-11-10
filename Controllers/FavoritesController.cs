using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.Services;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

[Authorize(Roles = AppRoles.Listener)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class FavoritesController(ApplicationDbContext db, VipEntitlements vip, AudioProcessingService audio) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Challenge();

        var saved = await db.FavoriteSongs.AsNoTracking()
            .Where(x => x.UserId == userId && x.Song.Status == SongStatus.Approved)
            .Include(x => x.Song).ThenInclude(x => x.Genre)
            .OrderByDescending(x => x.SavedAt).ThenByDescending(x => x.SongId)
            .ToListAsync(cancellationToken);
        var songs = saved.Select(x => x.Song).ToList();
        var flacSongIds = songs.Where(x => audio.FindStoredFile(x.FlacFileName) is not null)
            .Select(x => x.Id).ToHashSet();

        return View(new CatalogViewModel
        {
            Songs = songs,
            FavoriteSongIds = saved.Select(x => x.SongId).ToHashSet(),
            FlacSongIds = flacSongIds,
            HasActiveVip = flacSongIds.Count > 0 && await vip.HasActiveListenerAsync(User, cancellationToken)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Set(int id, [BindRequired] bool isFavorite, string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || !ModelState.IsValid) return BadRequest();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Challenge();

        var favorite = db.FavoriteSongs.Where(x => x.UserId == userId && x.SongId == id);
        if (isFavorite)
        {
            var isApproved = await db.Songs.AnyAsync(x => x.Id == id && x.Status == SongStatus.Approved,
                cancellationToken);
            if (!isApproved) return NotFound();

            if (!await favorite.AnyAsync(cancellationToken))
            {
                db.FavoriteSongs.Add(new FavoriteSong { UserId = userId, SongId = id, SavedAt = DateTimeOffset.UtcNow });
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 })
                {
                    // A concurrent save already added this user and song.
                }
            }
        }
        else
        {
            await favorite.ExecuteDeleteAsync(cancellationToken);
        }

        if (Request.GetTypedHeaders().Accept?.Any(x => x.MediaType.Value == "application/json") == true)
            return Json(new { isFavorite });

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }
}
