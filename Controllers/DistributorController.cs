using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.Services;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

[Authorize(Roles = AppRoles.Distributor)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class DistributorController(ApplicationDbContext db, MediaUploadService media,
    ILogger<DistributorController> logger, UploadProgressStore progressStore) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var source = await CurrentSourceAsync();
        if (source is null) return Forbid();
        return View(new DistributorDashboardViewModel
        {
            Source = source,
            Songs = await db.Songs.AsNoTracking().Include(x => x.Genre)
                .Where(x => x.DistributorId == source.Id).OrderByDescending(x => x.CreatedAt).ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        var source = await CurrentSourceAsync();
        return source is null ? Forbid() : View(await PopulateAsync(new DistributorUploadViewModel(), source));
    }

    [HttpPost, RequestSizeLimit(80 * 1024 * 1024),
        RequestFormLimits(MultipartBodyLengthLimit = 80 * 1024 * 1024)]
    public async Task<IActionResult> Upload(DistributorUploadViewModel model, CancellationToken cancellationToken)
    {
        // Source and submitter always come from the authenticated account, never form fields.
        var source = await CurrentSourceAsync();
        if (source is null) return Forbid();
        var progress = progressStore.Claim(model.UploadId, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!string.IsNullOrEmpty(model.UploadId) && progress is null)
        {
            ModelState.AddModelError(string.Empty, "Lượt tải không hợp lệ hoặc đã được sử dụng. Vui lòng gửi lại.");
            return BadRequest(new { errors = UploadResponses.Errors(ModelState) });
        }
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
            progress?.CancellationToken ?? CancellationToken.None);
        cancellationToken = linked.Token;
        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Artist = model.Artist?.Trim() ?? string.Empty;
        model.RightsHolder = model.RightsHolder?.Trim() ?? string.Empty;
        model.RightsEvidence = model.RightsEvidence?.Trim() ?? string.Empty;
        model.ExternalRecordingCode = NormalizeCode(model.ExternalRecordingCode);
        model.ExternalReleaseCode = NormalizeCode(model.ExternalReleaseCode);
        ValidateLength(nameof(model.Title), model.Title, 2, 160);
        ValidateLength(nameof(model.Artist), model.Artist, 2, 120);
        ValidateLength(nameof(model.RightsHolder), model.RightsHolder, 2, 160);
        ValidateLength(nameof(model.RightsEvidence), model.RightsEvidence, 10, 2000);
        if (!await db.Genres.AnyAsync(x => x.Id == model.GenreId))
            ModelState.AddModelError(nameof(model.GenreId), "Thể loại không tồn tại.");
        if (model.ExternalRecordingCode is not null && await db.Songs.AnyAsync(x =>
                x.DistributorId == source.Id && x.ExternalRecordingCode == model.ExternalRecordingCode))
            ModelState.AddModelError(nameof(model.ExternalRecordingCode), "Mã bản ghi này đã được gửi từ đơn vị của bạn.");
        foreach (var (field, message) in await media.ValidateAsync(model))
            ModelState.AddModelError(field, message);
        if (!ModelState.IsValid)
        {
            progress?.Fail("Thông tin chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu.");
            return UploadResponses.WantsJson(Request) ? BadRequest(new { errors = UploadResponses.Errors(ModelState) })
                : View(await PopulateAsync(model, source));
        }

        ProcessedAudio? audio = null;
        string? cover = null;
        var saved = false;
        try
        {
            audio = await media.ProcessFlacAsync(model.FlacFile!, cancellationToken, progress is null ? null : progress.Report);
            cover = audio.EmbeddedCoverFileName;
            if (model.CoverFile is not null) cover = await media.SaveAsync(model.CoverFile, cover: true, cancellationToken: cancellationToken);
            if (audio.EmbeddedCoverFileName != cover) media.DeleteCreatedFile(audio.EmbeddedCoverFileName, cover: true);
            progress?.Saving();
            var song = new Song
            {
                Title = model.Title, Artist = model.Artist,
                GenreId = model.GenreId, DurationSeconds = audio.DurationSeconds,
                ImportedById = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                DistributorId = source.Id, ExternalRecordingCode = model.ExternalRecordingCode,
                ExternalReleaseCode = model.ExternalReleaseCode,
                AudioFileName = audio.Mp3FileName, HighQualityFileName = audio.HighQualityFileName,
                FlacFileName = audio.FlacFileName, CoverFileName = cover,
                CoverPath = cover is null ? "/images/covers/saigon-dawn.svg" : $"/Songs/Cover/{cover}",
                RightsHolder = model.RightsHolder, RightsEvidence = model.RightsEvidence,
                Status = SongStatus.Pending
            };
            db.Songs.Add(song);
            await db.SaveChangesAsync();
            saved = true;
            progress?.Complete();
            TempData["Success"] = $"Đã gửi {song.ReferenceCode}. Bài hát đang chờ admin kiểm tra và duyệt.";
            if (UploadResponses.WantsJson(Request))
                return Json(new { redirectUrl = Url.Action(nameof(Details), new { id = song.Id }) });
            return RedirectToAction(nameof(Details), new { id = song.Id });
        }
        catch (MediaProcessingException ex)
        {
            progress?.Fail(ex.Message);
            ModelState.AddModelError(nameof(model.FlacFile), ex.Message);
            return UploadResponses.WantsJson(Request) ? BadRequest(new { errors = UploadResponses.Errors(ModelState) })
                : View(await PopulateAsync(model, source));
        }
        catch (OperationCanceledException)
        {
            progress?.Cancel();
            throw;
        }
        catch (Exception ex) when (ex is IOException or DbUpdateException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Cannot save a distributor submission from source {DistributorId}", source.Id);
            ModelState.AddModelError(string.Empty, "Chưa lưu được bài hát hoặc mã bản ghi bị trùng. Vui lòng kiểm tra thông tin và chọn lại tệp.");
            progress?.Fail("Chưa lưu được bài hát. Vui lòng kiểm tra thông tin và thử lại.");
            return UploadResponses.WantsJson(Request) ? BadRequest(new { errors = UploadResponses.Errors(ModelState) })
                : View(await PopulateAsync(model, source));
        }
        finally
        {
            if (!saved)
            {
                media.DeleteCreatedFile(audio?.Mp3FileName);
                media.DeleteCreatedFile(audio?.HighQualityFileName);
                media.DeleteCreatedFile(audio?.FlacFileName);
                media.DeleteCreatedFile(audio?.EmbeddedCoverFileName, cover: true);
                media.DeleteCreatedFile(cover, cover: true);
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var source = await CurrentSourceAsync();
        if (source is null) return Forbid();
        var song = await db.Songs.AsNoTracking().Include(x => x.Genre).Include(x => x.Distributor)
            .Include(x => x.ImportedBy).Include(x => x.Reviewer)
            .FirstOrDefaultAsync(x => x.Id == id && x.DistributorId == source.Id);
        return song is null ? NotFound() : View(song);
    }

    private async Task<Distributor?> CurrentSourceAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await db.Users.AsNoTracking().Where(x => x.Id == userId && x.DistributorId != null)
            .Select(x => x.Distributor).FirstOrDefaultAsync();
    }

    private async Task<DistributorUploadViewModel> PopulateAsync(DistributorUploadViewModel model, Distributor source)
    {
        model.Source = source;
        model.Genres = await db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        return model;
    }

    private static string? NormalizeCode(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    private void ValidateLength(string field, string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
            ModelState.AddModelError(field, $"Nội dung cần từ {minimum} đến {maximum} ký tự sau khi bỏ khoảng trắng.");
    }
}
