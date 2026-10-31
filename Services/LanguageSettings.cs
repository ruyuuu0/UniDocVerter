using System.Globalization;
using System.Text.Json;

namespace UniDocVerter.Services;

public sealed class LanguageSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UniDocVerter");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDirectory, "settings.json");

    public string Language { get; private set; }

    private LanguageSettings(string language)
    {
        Language = language;
    }

    public static LanguageSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(SettingsPath));

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!string.Equals(
                            property.Name,
                            "language",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string? savedLanguage = property.Value.GetString();

                    if (savedLanguage is "ru" or "en")
                        return new LanguageSettings(savedLanguage);
                }
            }
        }
        catch
        {
            // Повреждённые настройки игнорируем и используем язык системы.
        }

        string systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return new LanguageSettings("en");
    }

    public void SetLanguage(string language)
    {
        if (language is not ("ru" or "en"))
            throw new ArgumentException("Unsupported language.", nameof(language));

        Language = language;
        Directory.CreateDirectory(SettingsDirectory);

        string json = JsonSerializer.Serialize(new SettingsFile(language), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(SettingsPath, json);
    }

    private sealed record SettingsFile(string Language);
}