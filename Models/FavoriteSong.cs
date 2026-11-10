namespace SoulSignal.Models;

public class FavoriteSong
{
    public string UserId { get; set; } = "";
    public int SongId { get; set; }
    public Song Song { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; }
}
