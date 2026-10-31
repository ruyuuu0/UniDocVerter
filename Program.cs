using System.ComponentModel;
using UniDocVerter.Services;

Console.WriteLine("UniDocVerter — локальный конвертер");
Console.WriteLine();

string input = args.Length > 0
    ? args[0].Trim('"')
    : ReadInputPath();

if (!File.Exists(input))
{
    Console.WriteLine("Файл не найден.");
    Pause();
    return;
}

input = Path.GetFullPath(input);

try
{
string ffprobePath = ToolLocator.Find("ffprobe");
string ffmpegPath = ToolLocator.Find("ffmpeg");

var probeService = new FfprobeService(ffprobePath);
var converter = new MediaConverter(ffmpegPath);

    var probe = await probeService.ProbeAsync(input);
    var formats = converter.GetAllowedFormats(probe);

    if (formats.Count == 0)
    {
        Console.WriteLine("Не найдены поддерживаемые аудио- или видеопотоки.");
        Pause();
        return;
    }

    Console.WriteLine($"Файл: {Path.GetFileName(input)}");
    Console.WriteLine(
        $"Найдено: {(probe.HasVideo ? "видео/изображение " : "")}" +
        $"{(probe.HasAudio ? "аудио" : "")}");
    Console.WriteLine($"Доступные форматы: {string.Join(", ", formats)}");
    Console.Write("Формат результата: ");

    string extension = Console.ReadLine()?.Trim().TrimStart('.').ToLowerInvariant() ?? "";

    if (!formats.Contains(extension, StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine("Этот формат недоступен для выбранного файла.");
        Pause();
        return;
    }

    string output = Path.Combine(
        Path.GetDirectoryName(input)!,
        $"{Path.GetFileNameWithoutExtension(input)}_converted.{extension}");

    if (File.Exists(output))
    {
        Console.WriteLine($"Файл уже существует: {output}");
        Pause();
        return;
    }

    await converter.ConvertAsync(input, output, extension, probe);

    Console.WriteLine($"Готово: {output}");
}
catch (Win32Exception)
{
    Console.WriteLine("Не найден FFmpeg или FFprobe. Проверь установку и PATH.");
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
}

Pause();

static string ReadInputPath()
{
    Console.Write("Путь к файлу: ");
    return Console.ReadLine()?.Trim().Trim('"') ?? "";
}

static void Pause()
{
    Console.WriteLine();
    Console.WriteLine("Нажми Enter, чтобы закрыть окно.");
    Console.ReadLine();
}