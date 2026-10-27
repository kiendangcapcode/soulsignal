using SoulSignal.ViewModels;

namespace SoulSignal.Services;

public class MediaUploadService(IWebHostEnvironment environment, AudioProcessingService audio)
{
    public const long MaxFlac = 50 * 1024 * 1024;
    public const long MaxCover = 2 * 1024 * 1024;

    public async Task<List<(string Field, string Message)>> ValidateAsync(CreateSongViewModel model)
    {
        var errors = new List<(string, string)>();
        await CheckAsync(model.FlacFile, nameof(model.FlacFile), ".flac", MaxFlac, errors);
        if (model.CoverFile is not null)
            await CheckAsync(model.CoverFile, nameof(model.CoverFile), "image", MaxCover, errors);
        return errors;
    }

    private static async Task CheckAsync(IFormFile? file, string field, string kind, long limit,
        List<(string, string)> errors)
    {
        if (file is null) return; // Required fields are handled by model validation.
        if (file.Length <= 0 || file.Length > limit)
        {
            errors.Add((field, $"Tệp cần có nội dung và không vượt quá {limit / 1024 / 1024} MB."));
            return;
        }
        var minimumLength = kind == ".flac" ? 64 : 45;
        if (file.Length < minimumLength)
        {
            errors.Add((field, "Tệp quá ngắn hoặc bị cắt, không đủ dữ liệu nội dung."));
            return;
        }
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var validExtension = kind == "image" ? extension is ".png" or ".jpg" or ".jpeg" : extension == kind;
        if (!validExtension)
        {
            errors.Add((field, kind == "image" ? "Ảnh bìa chỉ nhận PNG hoặc JPG." : $"Cần đúng tệp {kind}."));
            return;
        }
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var length = await stream.ReadAtLeastAsync(header, 12, throwOnEndOfStream: false);
        var valid = kind switch
        {
            ".flac" => length >= 4 && header[0] == 'f' && header[1] == 'L' && header[2] == 'a' && header[3] == 'C',
            "image" when extension == ".png" => length >= 8 && header.AsSpan(0, 8)
                .SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image" => length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            _ => false
        };
        if (!valid) errors.Add((field, "Nội dung tệp không khớp định dạng. Đổi tên đuôi tệp không tạo ra định dạng hợp lệ."));
    }

    public async Task<ProcessedAudio> ProcessFlacAsync(IFormFile file, CancellationToken cancellationToken = default,
        Action<MediaProcessingProgress>? reportProgress = null)
    {
        // Do not rely on the controller having called form validation first.
        var errors = new List<(string, string)>();
        await CheckAsync(file, "FlacFile", ".flac", MaxFlac, errors);
        if (errors.Count != 0) throw new MediaProcessingException(errors[0].Item2);
        var original = await SaveAsync(file, cancellationToken: cancellationToken);
        try
        {
            return await audio.ProcessAsync(original, cancellationToken, reportProgress);
        }
        catch
        {
            DeleteCreatedFile(original);
            throw;
        }
    }

    public async Task<string> SaveAsync(IFormFile file, bool cover = false, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(environment.ContentRootPath, "Media", cover ? "Covers" : "Audio");
        Directory.CreateDirectory(directory);
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        var path = Path.Combine(directory, name);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            await file.CopyToAsync(output, cancellationToken);
            return name;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    public void DeleteCreatedFile(string? name, bool cover = false)
    {
        if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name) return;
        var path = Path.Combine(environment.ContentRootPath, "Media", cover ? "Covers" : "Audio", name);
        if (File.Exists(path)) File.Delete(path);
    }
}
