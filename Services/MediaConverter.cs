using System.Diagnostics;
using UniDocVerter.Models;

namespace UniDocVerter.Services;

public sealed class MediaConverter
{
    private readonly string _ffmpegPath;

    private static readonly HashSet<string> AudioFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "mp3", "wav", "aac", "opus", "m4a"
        };

    private static readonly HashSet<string> ImageFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "png", "jpg", "webp"
        };

    private static readonly HashSet<string> VideoFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "mp4", "webm", "mkv", "gif"
        };

    public MediaConverter(string ffmpegPath = "ffmpeg")
    {
        _ffmpegPath = ffmpegPath;
    }

    public IReadOnlyList<string> GetAllowedFormats(MediaProbeResult probe)
    {
        var formats = new List<string>();

        if (probe.HasVideo)
            formats.AddRange(ImageFormats.Concat(VideoFormats));

        if (probe.HasAudio)
            formats.AddRange(AudioFormats);

        return formats.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task ConvertAsync(
        string input,
        string output,
        string extension,
        MediaProbeResult probe,
        CancellationToken cancellationToken = default)
    {
        extension = extension.TrimStart('.').ToLowerInvariant();

        if (!GetAllowedFormats(probe).Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Формат .{extension} не подходит для потоков в этом файле.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(input);

        if (AudioFormats.Contains(extension))
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add("0:a:0");
            startInfo.ArgumentList.Add("-vn");
        }
        else if (extension == "gif")
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add("0:v:0");
            startInfo.ArgumentList.Add("-an");
        }
        else if (ImageFormats.Contains(extension))
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add("0:v:0");
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-an");
        }
        else
        {
            startInfo.ArgumentList.Add("-map");
            startInfo.ArgumentList.Add("0:v:0");

            if (probe.HasAudio)
            {
                startInfo.ArgumentList.Add("-map");
                startInfo.ArgumentList.Add("0:a?");
            }
            else
            {
                startInfo.ArgumentList.Add("-an");
            }
        }

        startInfo.ArgumentList.Add(output);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить FFmpeg.");

        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "FFmpeg завершился с ошибкой."
                    : error);
        }
    }
}