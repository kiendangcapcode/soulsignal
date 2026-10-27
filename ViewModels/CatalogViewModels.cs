using SoulSignal.Models;

namespace SoulSignal.ViewModels;

public class CatalogViewModel
{
    public IReadOnlyList<Song> Songs { get; set; } = [];
    public IReadOnlyList<Genre> Genres { get; set; } = [];
    public string? Query { get; set; }
    public int? GenreId { get; set; }
}

public class SongDetailsViewModel
{
    public Song Song { get; set; } = null!;
}

public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int DistributorCount { get; set; }
    public int ApprovedSongCount { get; set; }
    public int PendingSongCount { get; set; }
    public IReadOnlyList<Song> RecentSongs { get; set; } = [];
    public IReadOnlyList<Distributor> Distributors { get; set; } = [];
}
