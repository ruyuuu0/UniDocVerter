using System.Diagnostics;
using System.Text.Json;
using UniDocVerter.Models;

namespace UniDocVerter.Services;

public sealed class FfprobeService
{
    private readonly string _ffprobePath;

    public FfprobeService(string ffprobePath = "ffprobe")
    {
        _ffprobePath = ffprobePath;
    }

    public async Task<MediaProbeResult> ProbeAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("stream=codec_type");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("json");
        startInfo.ArgumentList.Add(input);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Не удалось запустить FFprobe.");

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? "FFprobe не смог прочитать файл."
                    : error);
        }

        using var document = JsonDocument.Parse(output);

        var streams = document.RootElement
            .GetProperty("streams")
            .EnumerateArray()
            .Select(stream => new MediaStream(
                stream.TryGetProperty("codec_type", out var type)
                    ? type.GetString() ?? ""
                    : ""))
            .ToList();

        return new MediaProbeResult(streams);
    }
}