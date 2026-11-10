using SoulSignal.Models;

namespace SoulSignal.ViewModels;

public class CatalogViewModel
{
    public IReadOnlyList<Song> Songs { get; set; } = [];
    public IReadOnlyList<Genre> Genres { get; set; } = [];
    public string? Query { get; set; }
    public string? Artist { get; set; }
    public int? GenreId { get; set; }
    public HashSet<int> FlacSongIds { get; set; } = [];
    public HashSet<int> FavoriteSongIds { get; set; } = [];
    public bool HasActiveVip { get; set; }
}

public record PlaybackLinkViewModel(
    Song Song, bool CanStreamFlac, bool CanDownloadFlac, bool Compact, bool HasFlac, bool IsFavorite = false);

public class SongDetailsViewModel
{
    public Song Song { get; set; } = null!;
    public bool HasFlac { get; set; }
    public bool CanStreamFlac { get; set; }
    public bool CanDownloadFlac { get; set; }
    public bool IsFavorite { get; set; }
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
