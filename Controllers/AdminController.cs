using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoulSignal.Data;
using SoulSignal.Models;
using SoulSignal.Services;
using SoulSignal.ViewModels;

namespace SoulSignal.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class AdminController(ApplicationDbContext db, MediaUploadService media, ILogger<AdminController> logger,
    UploadProgressStore progressStore) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(new AdminDashboardViewModel
    {
        UserCount = await db.Users.CountAsync(),
        DistributorCount = await db.Distributors.CountAsync(),
        ApprovedSongCount = await db.Songs.CountAsync(x => x.Status == SongStatus.Approved),
        PendingSongCount = await db.Songs.CountAsync(x => x.Status == SongStatus.Pending),
        Distributors = await db.Distributors.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
        RecentSongs = await db.Songs.AsNoTracking().Include(x => x.Genre).Include(x => x.Distributor)
            .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync()
    });

    [HttpGet]
    public async Task<IActionResult> Distributors() => View(await db.Distributors.AsNoTracking()
        .OrderBy(x => x.Name).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> ReceiveDemo(int distributorId)
    {
        var source = await db.Distributors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == distributorId);
        if (source is null || !source.IsSimulation) return BadRequest("Nguồn mô phỏng không hợp lệ.");
        var code = $"SSDEMO-{source.Code}-REC-001";
        var existing = await db.Songs.AsNoTracking().FirstOrDefaultAsync(x =>
            x.DistributorId == distributorId && x.ExternalRecordingCode == code);
        if (existing is not null)
        {
            TempData["Success"] = "Gói mẫu này đã được nhận. Mở lại hồ sơ hiện có, không tạo trùng.";
            return RedirectToAction(nameof(ReviewSong), new { id = existing.Id });
        }
        var genreId = await db.Genres.OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
        var song = new Song
        {
            Title = $"Gói nhạc mẫu — {source.Name}", Artist = "Soul Signal Studio",
            Description = "Gói phân phối giả lập dùng tài nguyên Saigon Dawn có sẵn. Không có kết nối tới công ty hoặc máy chủ bên ngoài.",
            GenreId = genreId, DistributorId = source.Id,
            ImportedById = User.FindFirstValue(ClaimTypes.NameIdentifier)!, DurationSeconds = 24,
            AudioFileName = "saigon-dawn.mp3", FlacFileName = "saigon-dawn.flac",
            CoverPath = "/images/covers/saigon-dawn.svg", ExternalRecordingCode = code,
            ExternalReleaseCode = $"SSDEMO-{source.Code}-REL-001",
            RightsHolder = "Soul Signal Studio (dữ liệu mẫu)",
            RightsEvidence = "Tệp nhạc tổng hợp riêng bằng scripts/generate-demo-audio.py; ảnh SVG riêng của đồ án.",
            Status = SongStatus.Pending
        };
        db.Songs.Add(song);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            var concurrent = await db.Songs.AsNoTracking().FirstOrDefaultAsync(x =>
                x.DistributorId == distributorId && x.ExternalRecordingCode == code);
            if (concurrent is null) throw;
            return RedirectToAction(nameof(ReviewSong), new { id = concurrent.Id });
        }
        TempData["Success"] = $"Đã nhận {song.ReferenceCode}. Kiểm tra mã nguồn và thông tin rồi duyệt.";
        return RedirectToAction(nameof(ReviewSong), new { id = song.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Import() => View(await PopulateAsync(new ImportReleaseViewModel()));

    [HttpPost, RequestSizeLimit(80 * 1024 * 1024),
        RequestFormLimits(MultipartBodyLengthLimit = 80 * 1024 * 1024)]
    public async Task<IActionResult> Import(ImportReleaseViewModel model, CancellationToken cancellationToken)
    {
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
        if (!await db.Distributors.AnyAsync(x => x.Id == model.DistributorId))
            ModelState.AddModelError(nameof(model.DistributorId), "Nguồn phân phối không tồn tại.");
        if (model.ExternalRecordingCode is not null && await db.Songs.AnyAsync(x =>
                x.DistributorId == model.DistributorId && x.ExternalRecordingCode == model.ExternalRecordingCode))
            ModelState.AddModelError(nameof(model.ExternalRecordingCode), "Mã bản ghi này đã được nhận từ đơn vị đã chọn.");
        foreach (var (field, message) in await media.ValidateAsync(model))
            ModelState.AddModelError(field, message);
        if (!ModelState.IsValid)
        {
            progress?.Fail("Thông tin chưa hợp lệ. Vui lòng kiểm tra các trường được đánh dấu.");
            return UploadResponses.WantsJson(Request) ? BadRequest(new { errors = UploadResponses.Errors(ModelState) })
                : View(await PopulateAsync(model));
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
                DistributorId = model.DistributorId, ExternalRecordingCode = model.ExternalRecordingCode,
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
            TempData["Success"] = $"Đã nhận gói {song.ReferenceCode}. Kiểm tra thông tin rồi duyệt công khai.";
            if (UploadResponses.WantsJson(Request))
                return Json(new { redirectUrl = Url.Action(nameof(ReviewSong), new { id = song.Id }) });
            return RedirectToAction(nameof(ReviewSong), new { id = song.Id });
        }
        catch (MediaProcessingException ex)
        {
            progress?.Fail(ex.Message);
            ModelState.AddModelError(nameof(model.FlacFile), ex.Message);
            return UploadResponses.WantsJson(Request) ? BadRequest(new { errors = UploadResponses.Errors(ModelState) })
                : View(await PopulateAsync(model));
        }
        catch (OperationCanceledException)
        {
            progress?.Cancel();
            throw;
        }
        catch (Exception ex) when (ex is IOException or DbUpdateException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Cannot import a simulated music delivery");
            ModelState.AddModelError(string.Empty, "Chưa lưu được gói nhạc hoặc mã bản ghi bị trùng. Vui lòng chọn lại tệp và kiểm tra thông tin.");
            progress?.Fail("Chưa lưu được bài hát. Vui lòng kiểm tra thông tin và thử lại.");
            return UploadResponses.WantsJson(Request) ? BadRequest(new { errors = UploadResponses.Errors(ModelState) })
                : View(await PopulateAsync(model));
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
    public async Task<IActionResult> ReviewSong(int id)
    {
        var song = await FindSongAsync(id);
        return song is null ? NotFound() : View(new ReviewSongViewModel { Id = id, Song = song });
    }

    [HttpPost]
    public async Task<IActionResult> ReviewSong(int id, ReviewSongViewModel model)
    {
        var song = await FindSongAsync(id);
        if (song is null) return NotFound();
        model.Id = id;
        model.Song = song;
        model.ReviewNote = model.ReviewNote?.Trim();
        if (model.Decision is not ("Approve" or "Reject"))
            ModelState.AddModelError(nameof(model.Decision), "Vui lòng chọn duyệt hoặc từ chối.");
        if (model.Decision == "Reject" && string.IsNullOrWhiteSpace(model.ReviewNote))
            ModelState.AddModelError(nameof(model.ReviewNote), "Cần nhập lý do từ chối.");
        if (model.Decision == "Approve" && (string.IsNullOrWhiteSpace(song.RightsHolder)
            || string.IsNullOrWhiteSpace(song.RightsEvidence)))
            ModelState.AddModelError(string.Empty, "Chưa có đủ thông tin về chủ thể và phạm vi quyền sử dụng.");
        if (!ModelState.IsValid) return View(model);
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var count = await db.Songs.Where(x => x.Id == id && x.Status == SongStatus.Pending)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Status, model.Decision == "Approve" ? SongStatus.Approved : SongStatus.Rejected)
                .SetProperty(x => x.ReviewNote, model.ReviewNote)
                .SetProperty(x => x.ReviewedAt, DateTimeOffset.UtcNow)
                .SetProperty(x => x.ReviewedById, reviewerId));
        TempData["Success"] = count == 0 ? "Gói đã được xử lý trước đó. Quyết định không bị thay đổi."
            : model.Decision == "Approve" ? $"Đã công khai {song.ReferenceCode} trên Soul Signal."
            : $"Đã từ chối {song.ReferenceCode}. Lý do được lưu trong hồ sơ gói nhạc.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Song?> FindSongAsync(int id) => await db.Songs.AsNoTracking()
        .Include(x => x.Genre).Include(x => x.Distributor).Include(x => x.ImportedBy).Include(x => x.Reviewer)
        .FirstOrDefaultAsync(x => x.Id == id);

    private async Task<ImportReleaseViewModel> PopulateAsync(ImportReleaseViewModel model)
    {
        model.Genres = await db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        model.Distributors = await db.Distributors.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        return model;
    }

    private static string? NormalizeCode(string? code) => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    private void ValidateLength(string field, string value, int minimum, int maximum)
    {
        if (value.Length < minimum || value.Length > maximum)
            ModelState.AddModelError(field, $"Nội dung cần từ {minimum} đến {maximum} ký tự sau khi bỏ khoảng trắng.");
    }
}
