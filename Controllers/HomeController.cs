using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? genreId)
    {
        q = q?.Trim();
        if (q?.Length > 120) q = q[..120];
        var songs = db.Songs.AsNoTracking().Include(x => x.Genre).Include(x => x.Distributor)
            .Where(x => x.Status == SongStatus.Approved);
        if (!string.IsNullOrWhiteSpace(q))
            songs = songs.Where(x => x.Title.Contains(q) || x.Artist.Contains(q));
        if (genreId.HasValue) songs = songs.Where(x => x.GenreId == genreId);
        return View(new CatalogViewModel
        {
            Songs = await songs.OrderBy(x => x.Id).Take(100).ToListAsync(),
            Genres = await db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            Query = q, GenreId = genreId
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
