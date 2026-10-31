using System.ComponentModel;

namespace UniDocVerter.Services;

public static class ToolLocator
{
    public static string Find(string toolName)
    {
        string executableName = OperatingSystem.IsWindows()
            ? $"{toolName}.exe"
            : toolName;

        string appDirectory = AppContext.BaseDirectory;
        string besideApp = Path.Combine(appDirectory, executableName);

        if (File.Exists(besideApp))
            return besideApp;

        string? fromPath = FindInPath(executableName);

        if (fromPath is not null)
            return fromPath;

        throw new Win32Exception(
            $"Не найден {executableName}. Положи его рядом с UniDocVerter.exe " +
            "или установи FFmpeg и добавь его в PATH.");
    }

    private static string? FindInPath(string executableName)
    {
        string? pathValue = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(pathValue))
            return null;

        foreach (string directory in pathValue.Split(Path.PathSeparator))
        {
            string candidate = Path.Combine(directory.Trim('"'), executableName);

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}