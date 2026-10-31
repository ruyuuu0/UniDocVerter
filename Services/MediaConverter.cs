using System.Diagnostics;
using System.Globalization;
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
        IProgress<int>? progress = null,
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
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add("-nostats");
        startInfo.ArgumentList.Add("-progress");
        startInfo.ArgumentList.Add("pipe:1");
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

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // The process could have ended simultaneously with the cancellation.
            }
        });

        try
        {
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                if (!line.StartsWith("out_time_us=", StringComparison.Ordinal))
                    continue;

                string value = line["out_time_us=".Length..];

                if (probe.Duration is not { TotalMilliseconds: > 0 } duration ||
                    !long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long microseconds))
                {
                    continue;
                }

                double percent = microseconds / (duration.TotalMilliseconds * 1000d) * 100d;
                progress?.Report((int)Math.Clamp(percent, 0, 99));
            }

            await process.WaitForExitAsync(cancellationToken);
            string error = await errorTask;

            cancellationToken.ThrowIfCancellationRequested();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(error)
                        ? "FFmpeg завершился с ошибкой."
                        : error);
            }

            progress?.Report(100);
        }
        catch (OperationCanceledException)
        {
            TryDeletePartialOutput(output);
            throw;
        }
        catch
        {
            if (process.ExitCode != 0)
                TryDeletePartialOutput(output);

            throw;
        }
    }

    private static void TryDeletePartialOutput(string output)
    {
        try
        {
            if (File.Exists(output))
                File.Delete(output);
        }
        catch
        {
            // We don’t hide the main error if the temporary file couldn’t be deleted.
        }
    }
}