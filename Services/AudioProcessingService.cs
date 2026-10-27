using System.ComponentModel;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SoulSignal.Services;

public sealed record ProcessedAudio(string Mp3FileName, string HighQualityFileName,
    string FlacFileName, int DurationSeconds, string? EmbeddedCoverFileName = null);

public sealed record MediaProcessingProgress(string Phase, int? Percent, string Message);

public sealed class MediaProcessingException(string message, Exception? innerException = null)
    : Exception(message, innerException) { }

public class AudioProcessingService(IWebHostEnvironment environment, IConfiguration configuration,
    ILogger<AudioProcessingService> logger)
{
    public const int MaxDurationSeconds = 1800;
    private const int MaxToolOutputCharacters = 32768;
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan EncodeTimeout = TimeSpan.FromSeconds(120);

    public async Task<ProcessedAudio> ProcessAsync(string originalName, CancellationToken cancellationToken = default,
        Action<MediaProcessingProgress>? reportProgress = null)
    {
        if (Path.GetFileName(originalName) != originalName || Path.GetExtension(originalName) != ".flac")
            throw new MediaProcessingException("Tệp FLAC gốc không hợp lệ.");
        var directory = Path.Combine(environment.ContentRootPath, "Media", "Audio");
        var originalPath = Path.Combine(directory, originalName);
        var lowName = Guid.NewGuid().ToString("N") + "-128.mp3";
        var highName = Guid.NewGuid().ToString("N") + "-320.mp3";
        var lowPath = Path.Combine(directory, lowName);
        var highPath = Path.Combine(directory, highName);
        string? embeddedCover = null;
        try
        {
            reportProgress?.Invoke(new("probing", null, "Đang đọc định dạng và thời lượng FLAC…"));
            var original = await ProbeAsync(originalPath, cancellationToken);
            if (original.Codec != "flac" || original.Format != "flac")
                throw new MediaProcessingException("Cần tệp FLAC thật. Đổi tên đuôi tệp không chuyển đổi được định dạng.");
            if (original.Duration <= 0 || original.Duration > MaxDurationSeconds)
                throw new MediaProcessingException("Thời lượng tệp FLAC cần lớn hơn 0 và không vượt quá 30 phút.");

            // Decode the original once and create both real CBR MP3 renditions.
            // Force a local FLAC demuxer and file-only protocols; do not decode artwork with the audio.
            var arguments = new List<string>
            {
                "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-xerror",
                "-progress", "pipe:1", "-stats_period", "0.25",
                "-protocol_whitelist", "file", "-f", "flac", "-err_detect", "crccheck+explode",
                "-discard:v", "all",
                "-i", originalPath
            };
            AddMp3Output(arguments, lowPath, "128k");
            AddMp3Output(arguments, highPath, "320k");
            reportProgress?.Invoke(new("encoding", 0, "Đang tạo MP3 128 / 320 kbps…"));
            var previousPercent = -1;
            await RunToolAsync(ToolPath("FfmpegPath", "ffmpeg"), arguments, EncodeTimeout,
                "Không giải mã được toàn bộ tệp FLAC. Tệp có thể bị hỏng hoặc bị cắt; hãy chọn lại bản gốc.",
                cancellationToken, line =>
                {
                    if (!line.StartsWith("out_time_us=", StringComparison.Ordinal)
                        || !long.TryParse(line.AsSpan(12), NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds)
                        || microseconds < 0) return;
                    var percent = (int)Math.Clamp(Math.Floor(microseconds / (original.Duration * 10000)), 0, 99);
                    if (percent <= previousPercent) return;
                    previousPercent = percent;
                    reportProgress?.Invoke(new("encoding", percent, $"Đang tạo MP3 128 / 320 kbps: {percent}%"));
                });

            // STREAMINFO can claim a full duration even when the file is cut at a frame boundary.
            // Verify the resulting media rather than accepting a successful process exit alone.
            reportProgress?.Invoke(new("probing", null, "Đang kiểm tra các bản MP3 đã tạo…"));
            await ValidateOutputAsync(lowPath, original.Duration, 128000, cancellationToken);
            await ValidateOutputAsync(highPath, original.Duration, 320000, cancellationToken);
            reportProgress?.Invoke(new("cover", null, "Đang tìm và lấy ảnh bìa có sẵn trong FLAC…"));
            embeddedCover = await ExtractEmbeddedCoverAsync(originalPath, cancellationToken);
            return new ProcessedAudio(lowName, highName, originalName, (int)Math.Ceiling(original.Duration), embeddedCover);
        }
        catch
        {
            DeleteOutput(lowPath);
            DeleteOutput(highPath);
            if (embeddedCover is not null)
                DeleteOutput(Path.Combine(environment.ContentRootPath, "Media", "Covers", embeddedCover));
            throw;
        }
    }

    private async Task<string?> ExtractEmbeddedCoverAsync(string sourcePath, CancellationToken cancellationToken)
    {
        string? temporaryPath = null, coverPath = null;
        try
        {
            var picture = await FindFlacPictureAsync(sourcePath, cancellationToken);
            if (picture is null) return null;
            var directory = Path.Combine(environment.ContentRootPath, "Media", "Covers");
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + picture.Extension);
            await File.WriteAllBytesAsync(temporaryPath, picture.Data, cancellationToken);
            var probe = await RunToolAsync(ToolPath("FfprobePath", "ffprobe"), new[]
            {
                "-v", "error", "-protocol_whitelist", "file", "-select_streams", "v:0",
                "-show_entries", "stream=codec_name,width,height", "-of", "json", temporaryPath
            }, ProbeTimeout, "Không đọc được ảnh bìa nhúng.", cancellationToken);
            using var info = JsonDocument.Parse(probe);
            var streams = info.RootElement.GetProperty("streams");
            if (streams.GetArrayLength() != 1) return null;
            var image = streams[0];
            var width = image.GetProperty("width").GetInt32();
            var height = image.GetProperty("height").GetInt32();
            if (width <= 0 || height <= 0 || width > 10000 || height > 10000 || (long)width * height > 25000000)
                return null;
            var name = Guid.NewGuid().ToString("N") + ".jpg";
            coverPath = Path.Combine(directory, name);
            await RunToolAsync(ToolPath("FfmpegPath", "ffmpeg"), new[]
            {
                "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-xerror",
                "-protocol_whitelist", "file", "-i", temporaryPath,
                "-map", "0:v:0", "-frames:v", "1", "-an", "-sn", "-dn",
                "-vf", "scale=w='min(1600,iw)':h='min(1600,ih)':force_original_aspect_ratio=decrease",
                "-map_metadata", "-1", "-c:v", "mjpeg", "-q:v", "3", "-threads", "1", coverPath
            }, ProbeTimeout, "Không chuyển đổi được ảnh bìa nhúng.", cancellationToken);
            if (!File.Exists(coverPath) || new FileInfo(coverPath).Length is < 45 or > MediaUploadService.MaxCover)
                return null;
            var header = new byte[3];
            await using (var saved = File.OpenRead(coverPath))
                await saved.ReadExactlyAsync(header, cancellationToken);
            if (header[0] != 0xff || header[1] != 0xd8 || header[2] != 0xff) return null;
            coverPath = null; // The caller now owns this completed cover.
            return name;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is MediaProcessingException or IOException or UnauthorizedAccessException
            or JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            // An absent or damaged picture must not invalidate an otherwise playable audio recording.
            logger.LogWarning(ex, "Embedded FLAC artwork could not be used; the default cover will be used");
            return null;
        }
        finally
        {
            if (temporaryPath is not null) DeleteOutput(temporaryPath);
            if (coverPath is not null) DeleteOutput(coverPath);
        }
    }

    private static async Task<EmbeddedPicture?> FindFlacPictureAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken);
        if (!header.AsSpan().SequenceEqual("fLaC"u8)) return null;
        EmbeddedPicture? fallback = null;
        while (stream.Position + 4 <= stream.Length)
        {
            await stream.ReadExactlyAsync(header, cancellationToken);
            var last = (header[0] & 0x80) != 0;
            var kind = header[0] & 0x7f;
            var length = (header[1] << 16) | (header[2] << 8) | header[3];
            if (length > stream.Length - stream.Position) break;
            if (kind == 6 && length is >= 32 and <= (int)MediaUploadService.MaxCover + 65536)
            {
                var block = new byte[length];
                await stream.ReadExactlyAsync(block, cancellationToken);
                var picture = ParsePicture(block);
                if (picture?.IsFrontCover == true) return picture;
                fallback ??= picture;
            }
            else stream.Seek(length, SeekOrigin.Current);
            if (last) break;
        }
        return fallback;
    }

    private static EmbeddedPicture? ParsePicture(byte[] block)
    {
        var offset = 0;
        int ReadInt()
        {
            if (offset > block.Length - 4) throw new FormatException("Incomplete FLAC picture metadata.");
            var value = BinaryPrimitives.ReadUInt32BigEndian(block.AsSpan(offset, 4));
            offset += 4;
            if (value > int.MaxValue) throw new FormatException("Invalid FLAC picture metadata length.");
            return (int)value;
        }
        void Skip(int count)
        {
            if (count < 0 || count > block.Length - offset) throw new FormatException("Invalid FLAC picture field.");
            offset += count;
        }
        try
        {
            var type = ReadInt();
            Skip(ReadInt()); // MIME description is not trusted; inspect the actual payload signature.
            Skip(ReadInt());
            Skip(16); // Width, height, bit depth, indexed colours; actual dimensions are probed later.
            var size = ReadInt();
            if (size < 45 || size > MediaUploadService.MaxCover || size > block.Length - offset) return null;
            var payload = block.AsSpan(offset, size);
            var png = payload.Length >= 8 && payload[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var jpeg = payload[0] == 0xff && payload[1] == 0xd8 && payload[2] == 0xff;
            return png || jpeg ? new EmbeddedPicture(payload.ToArray(), png ? ".png" : ".jpg", type == 3) : null;
        }
        catch (FormatException) { return null; }
    }

    private static void AddMp3Output(List<string> arguments, string path, string bitrate)
    {
        arguments.AddRange(new[]
        {
            "-map", "0:a:0", "-vn", "-sn", "-dn", "-map_metadata", "-1", "-map_chapters", "-1",
            "-c:a", "libmp3lame", "-b:a", bitrate, "-ar", "44100", "-ac", "2", "-threads", "1", path
        });
    }

    private async Task ValidateOutputAsync(string path, double sourceDuration, int bitrate, CancellationToken cancellationToken)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < 128)
            throw new MediaProcessingException("Chưa tạo được bản MP3 từ FLAC. Vui lòng thử lại.");
        var output = await ProbeAsync(path, cancellationToken);
        // MP3 has a small encoder delay/padding; this tolerance is below a tenth of a second.
        if (output.Codec != "mp3" || output.BitRate != bitrate || Math.Abs(output.Duration - sourceDuration) > 0.1)
            throw new MediaProcessingException("Bản nhạc tạo ra không đủ thời lượng hoặc không đúng chất lượng. Tệp FLAC có thể bị cắt hoặc bị hỏng.");
    }

    private async Task<AudioInfo> ProbeAsync(string path, CancellationToken cancellationToken)
    {
        var output = await RunToolAsync(ToolPath("FfprobePath", "ffprobe"), new[]
        {
            "-v", "error", "-protocol_whitelist", "file", "-select_streams", "a:0",
            "-show_entries", "stream=codec_name,duration,bit_rate:format=duration,format_name", "-of", "json", path
        }, ProbeTimeout, "Không đọc được tệp âm thanh. Hãy chọn tệp FLAC gốc có thể phát đầy đủ.", cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;
            var streams = root.GetProperty("streams");
            if (streams.GetArrayLength() != 1) throw new FormatException("Missing audio stream.");
            var stream = streams[0];
            var format = root.GetProperty("format");
            var duration = ReadNumber(stream, "duration") ?? ReadNumber(format, "duration");
            if (duration is null || !double.IsFinite(duration.Value) || duration <= 0)
                throw new FormatException("Missing finite positive duration.");
            return new AudioInfo(stream.GetProperty("codec_name").GetString() ?? string.Empty,
                format.GetProperty("format_name").GetString() ?? string.Empty, duration.Value,
                (int?)ReadNumber(stream, "bit_rate"));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new MediaProcessingException("Không đọc được định dạng hoặc thời lượng hợp lệ của tệp âm thanh.", ex);
        }
    }

    private static double? ReadNumber(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    private string ToolPath(string key, string fallback) => configuration[$"MediaProcessing:{key}"] is { Length: > 0 } value
        ? value : fallback;

    private async Task<string> RunToolAsync(string executable, IEnumerable<string> arguments, TimeSpan timeout,
        string invalidMediaMessage, CancellationToken cancellationToken, Action<string>? onOutputLine = null)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            }
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        try
        {
            if (!process.Start()) throw new Win32Exception("Tool process did not start.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            logger.LogError(ex, "Audio processing tool could not be started");
            throw new MediaProcessingException("Máy chủ chưa chạy được FFmpeg/FFprobe. Hãy cài công cụ và kiểm tra cấu hình xử lý nhạc.", ex);
        }

        var stdout = ReadLimitedAsync(process.StandardOutput, linked.Token, onOutputLine);
        var stderr = ReadLimitedAsync(process.StandardError, linked.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
            var results = await Task.WhenAll(stdout, stderr);
            if (process.ExitCode != 0)
            {
                logger.LogWarning("Audio tool exited with code {ExitCode}: {ToolError}", process.ExitCode, results[1]);
                throw new MediaProcessingException(invalidMediaMessage);
            }
            if (!string.IsNullOrWhiteSpace(results[1]))
                logger.LogWarning("Audio tool reported non-fatal diagnostics: {ToolError}", results[1]);
            return results[0];
        }
        catch (OperationCanceledException)
        {
            await TerminateAsync(process);
            try { await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException or IOException) { }
            cancellationToken.ThrowIfCancellationRequested();
            throw new MediaProcessingException("Xử lý nhạc mất quá nhiều thời gian. Vui lòng chọn tệp khác hoặc thử lại.");
        }
    }

    private static async Task<string> ReadLimitedAsync(StreamReader reader, CancellationToken cancellationToken,
        Action<string>? onLine = null)
    {
        var text = new StringBuilder();
        if (onLine is not null)
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
            {
                onLine(line);
                var remaining = MaxToolOutputCharacters - text.Length;
                if (remaining > 0) text.AppendLine(line[..Math.Min(remaining, line.Length)]);
            }
            return text.ToString();
        }
        var buffer = new char[2048];
        int read;
        while ((read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
        {
            var remaining = MaxToolOutputCharacters - text.Length;
            if (remaining > 0) text.Append(buffer, 0, Math.Min(remaining, read));
        }
        return text.ToString();
    }

    private static async Task TerminateAsync(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) when (ex is TimeoutException or InvalidOperationException or Win32Exception) { }
    }

    private void DeleteOutput(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not remove a failed audio output");
        }
    }

    private sealed record AudioInfo(string Codec, string Format, double Duration, int? BitRate);
    private sealed record EmbeddedPicture(byte[] Data, string Extension, bool IsFrontCover);
}
