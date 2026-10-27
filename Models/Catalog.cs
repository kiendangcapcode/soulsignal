using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoulSignal.Models;

public class Genre
{
    public int Id { get; set; }
    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;
}

public enum SongStatus { Pending, Approved, Rejected }

public class Song
{
    public int Id { get; set; }
    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [Required, MaxLength(120)]
    public string Artist { get; set; } = string.Empty;
    [Required, MaxLength(1500)]
    public string Description { get; set; } = string.Empty;
    [Range(1, 86400)]
    public int DurationSeconds { get; set; }
    [Required, MaxLength(260)]
    public string CoverPath { get; set; } = string.Empty;
    [Required, MaxLength(120)]
    public string AudioFileName { get; set; } = string.Empty;
    [MaxLength(120)]
    public string? HighQualityFileName { get; set; }
    [MaxLength(120)]
    public string? FlacFileName { get; set; }
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
    [Required]
    public string ImportedById { get; set; } = string.Empty;
    public ApplicationUser ImportedBy { get; set; } = null!;
    public int? DistributorId { get; set; }
    public Distributor? Distributor { get; set; }
    [MaxLength(64)] public string? ExternalRecordingCode { get; set; }
    [MaxLength(64)] public string? ExternalReleaseCode { get; set; }
    public SongStatus Status { get; set; } = SongStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [NotMapped] public string ReferenceCode => $"SS-TRACK-{Id:000000}";
    [MaxLength(120)] public string? CoverFileName { get; set; }
    [MaxLength(160)] public string RightsHolder { get; set; } = string.Empty;
    [MaxLength(2000)] public string RightsEvidence { get; set; } = string.Empty;
    [MaxLength(1000)] public string? ReviewNote { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }
    public ApplicationUser? Reviewer { get; set; }
}
