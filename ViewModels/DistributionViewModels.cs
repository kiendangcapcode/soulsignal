using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SoulSignal.Models;

namespace SoulSignal.ViewModels;

public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is true;
}

public class CreateSongViewModel
{
    [StringLength(32)] public string? UploadId { get; set; }
    [Required, StringLength(160, MinimumLength = 2)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(120, MinimumLength = 2)] public string Artist { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thể loại.")] public int GenreId { get; set; }
    [ValidateNever] public IReadOnlyList<Genre> Genres { get; set; } = [];
    [Required(ErrorMessage = "Vui lòng chọn tệp FLAC gốc.")] public IFormFile? FlacFile { get; set; }
    public IFormFile? CoverFile { get; set; }
    [Required, StringLength(160, MinimumLength = 2)] public string RightsHolder { get; set; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 10)] public string RightsEvidence { get; set; } = string.Empty;
    [MustBeTrue(ErrorMessage = "Bạn cần xác nhận đủ quyền phát trực tuyến và cho tải theo phạm vi nền tảng.")]
    public bool AcceptRights { get; set; }
}

public class ReviewSongViewModel
{
    [ValidateNever] public Song Song { get; set; } = null!;
    public int Id { get; set; }
    [Required] public string Decision { get; set; } = string.Empty;
    [StringLength(1000)] public string? ReviewNote { get; set; }
}

public class ImportReleaseViewModel : CreateSongViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nguồn phân phối.")]
    public int DistributorId { get; set; }
    [ValidateNever] public IReadOnlyList<Distributor> Distributors { get; set; } = [];
    [StringLength(64)] public string? ExternalRecordingCode { get; set; }
    [StringLength(64)] public string? ExternalReleaseCode { get; set; }
}

public class DistributorDashboardViewModel
{
    public Distributor Source { get; set; } = null!;
    public IReadOnlyList<Song> Songs { get; set; } = [];
}

public class DistributorUploadViewModel : CreateSongViewModel
{
    [StringLength(64)] public string? ExternalRecordingCode { get; set; }
    [StringLength(64)] public string? ExternalReleaseCode { get; set; }
    [BindNever, ValidateNever] public Distributor Source { get; set; } = null!;
}
