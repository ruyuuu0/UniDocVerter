using System.ComponentModel;
using System.Diagnostics;
using UniDocVerter.Models;
using UniDocVerter.Services;

namespace UniDocVerter;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ConverterForm(args.FirstOrDefault()));
    }
}

internal sealed class ConverterForm : Form
{
    private readonly LanguageSettings _settings = LanguageSettings.Load();

    private readonly TextBox _filePath = new()
    {
        ReadOnly = true
    };

    private readonly Button _chooseButton = new();
    private readonly ComboBox _formatBox = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 130,
        Enabled = false
    };

    private readonly Button _convertButton = new()
    {
        Enabled = false
    };

    private readonly ComboBox _languageBox = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 125
    };

    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly Label _fileLabel = new();
    private readonly Label _formatLabel = new();
    private readonly Label _status = new();

    private string? _selectedFile;
    private MediaProbeResult? _probeResult;
    private bool _isBusy;
    private bool _changingLanguage;

    private bool IsRussian => _settings.Language == "ru";

    public ConverterForm(string? startupFile)
    {
        Text = "UniDocVerter";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 430);
        Size = new Size(760, 470);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(18, 22, 30);
        ForeColor = Color.FromArgb(232, 236, 243);
        Font = new Font("Segoe UI", 10F);
        
        AllowDrop = true;
        DragEnter += Form_DragEnter;
        DragDrop += Form_DragDrop;

        var languageCaption = new Label
        {
            Text = "Language / Язык",
            ForeColor = Color.FromArgb(158, 169, 184),
            Font = new Font("Segoe UI", 9F),
            AutoSize = true,
            Location = new Point(28, 94)
        };

        _languageBox.Items.AddRange(new object[] { "Русский", "English" });
        _languageBox.SelectedIndex = IsRussian ? 0 : 1;
        _languageBox.SetBounds(145, 88, 125, 30);
        _languageBox.BackColor = Color.FromArgb(38, 45, 58);
        _languageBox.ForeColor = Color.White;
        _languageBox.FlatStyle = FlatStyle.Flat;

        _title.Font = new Font("Segoe UI Semibold", 21F);
        _title.ForeColor = Color.White;
        _title.AutoSize = true;
        _title.Location = new Point(28, 24);

        _subtitle.Font = new Font("Segoe UI", 10F);
        _subtitle.ForeColor = Color.FromArgb(158, 169, 184);
        _subtitle.AutoSize = true;
        _subtitle.Location = new Point(31, 66);

        var card = new Panel
        {
            BackColor = Color.FromArgb(29, 35, 46),
            Location = new Point(28, 132),
            Size = new Size(688, 205),
            Padding = new Padding(20)
        };

        _fileLabel.Font = new Font("Segoe UI Semibold", 9F);
        _fileLabel.ForeColor = Color.FromArgb(158, 169, 184);
        _fileLabel.AutoSize = true;
        _fileLabel.Location = new Point(20, 20);

        _filePath.SetBounds(20, 52, 440, 34);
        _filePath.BackColor = Color.FromArgb(38, 45, 58);
        _filePath.ForeColor = Color.White;
        _filePath.BorderStyle = BorderStyle.FixedSingle;
        _filePath.Font = new Font("Segoe UI", 10F);

        StyleButton(_chooseButton, secondary: true);
        _chooseButton.SetBounds(474, 50, 174, 36);

        _formatLabel.Font = new Font("Segoe UI Semibold", 9F);
        _formatLabel.ForeColor = Color.FromArgb(158, 169, 184);
        _formatLabel.AutoSize = true;
        _formatLabel.Location = new Point(20, 112);

        _formatBox.SetBounds(20, 143, 190, 36);
        _formatBox.BackColor = Color.FromArgb(38, 45, 58);
        _formatBox.ForeColor = Color.White;
        _formatBox.FlatStyle = FlatStyle.Flat;
        _formatBox.Font = new Font("Segoe UI", 10F);

        StyleButton(_convertButton, secondary: false);
        _convertButton.SetBounds(474, 140, 174, 40);

        _progressBar.SetBounds(28, 350, 530, 18);
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Visible = false;

        StyleButton(_cancelButton, secondary: true);
        _cancelButton.Text = IsRussian ? "Отмена" : "Cancel";
        _cancelButton.SetBounds(578, 342, 130, 34);
        _cancelButton.Visible = false;

        _status.SetBounds(28, 390, 680, 40);
        _status.ForeColor = Color.FromArgb(158, 169, 184);
        _status.Font = new Font("Segoe UI", 9.5F);
        _status.AutoSize = false;

        card.Controls.Add(_fileLabel);
        card.Controls.Add(_filePath);
        card.Controls.Add(_chooseButton);
        card.Controls.Add(_formatLabel);
        card.Controls.Add(_formatBox);
        card.Controls.Add(_convertButton);

        Controls.Add(_title);
        Controls.Add(_subtitle);
        Controls.Add(languageCaption);
        Controls.Add(_languageBox);
        Controls.Add(card);
        Controls.Add(_status);
        Controls.Add(_progressBar);
        Controls.Add(_cancelButton);

        _chooseButton.Click += async (_, _) => await ChooseFileAsync();
        _convertButton.Click += async (_, _) => await ConvertAsync();
        _languageBox.SelectedIndexChanged += (_, _) => ChangeLanguage();
        _cancelButton.Click += (_, _) => _conversionCts?.Cancel();

        ApplyLanguage();
        if (!string.IsNullOrWhiteSpace(startupFile))
        {
            Shown += async (_, _) => await LoadFileAsync(startupFile);
        }
    }

    private void Form_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private async void Form_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files ||
            files.Length == 0)
        {
            return;
        }

        await LoadFileAsync(files[0]);
    }
    private void ChangeLanguage()
    {
        if (_changingLanguage || _languageBox.SelectedIndex < 0)
            return;

        string language = _languageBox.SelectedIndex == 0 ? "ru" : "en";

        try
        {
            _settings.SetLanguage(language);
            ApplyLanguage();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not save language settings:\n{ex.Message}",
                "UniDocVerter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ApplyLanguage()
    {
        _changingLanguage = true;

        try
        {
            _languageBox.SelectedIndex = IsRussian ? 0 : 1;

            _title.Text = IsRussian ? "Медиа-конвертер" : "Media Converter";
            _subtitle.Text = IsRussian
                ? "Локальная конвертация фото, аудио и видео"
                : "Convert images, audio and video locally";

            _fileLabel.Text = IsRussian ? "ИСХОДНЫЙ ФАЙЛ" : "SOURCE FILE";
            _formatLabel.Text = IsRussian ? "ФОРМАТ РЕЗУЛЬТАТА" : "OUTPUT FORMAT";
            _chooseButton.Text = IsRussian ? "Выбрать файл…" : "Choose file…";
            _convertButton.Text = IsRussian ? "Конвертировать" : "Convert";

            if (_selectedFile is null)
            {
                _status.Text = IsRussian
                    ? "Выбери изображение, аудио- или видеофайл."
                    : "Choose an image, audio or video file.";
            }
            else if (_probeResult is null)
            {
                _status.Text = IsRussian
                    ? "Файл ещё не проверен."
                    : "The file has not been checked yet.";
            }
            else if (_formatBox.Enabled)
            {
                _status.Text = IsRussian
                    ? "Файл проверен. Выбери формат и нажми «Конвертировать»."
                    : "File checked. Choose a format and click “Convert”.";
            }

            Text = "UniDocVerter";
        }
        finally
        {
            _changingLanguage = false;
        }
    }

    private async Task ChooseFileAsync()
{
    using var dialog = new OpenFileDialog
    {
        Title = IsRussian ? "Выбери медиафайл" : "Choose a media file",
        Filter = IsRussian
            ? "Медиафайлы|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.mp3;*.wav;*.aac;*.m4a;*.opus;*.mp4;*.webm;*.mkv;*.mov;*.avi|Все файлы|*.*"
            : "Media files|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.mp3;*.wav;*.aac;*.m4a;*.opus;*.mp4;*.webm;*.mkv;*.mov;*.avi|All files|*.*",
        CheckFileExists = true,
        Multiselect = false
    };

    if (dialog.ShowDialog(this) == DialogResult.OK)
        await LoadFileAsync(dialog.FileName);
}

    private async Task LoadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        _selectedFile = Path.GetFullPath(filePath);
        _probeResult = null;
        _filePath.Text = _selectedFile;
        _formatBox.Items.Clear();
        _formatBox.Enabled = false;
        _convertButton.Enabled = false;

        await RunBusyOperationAsync(async () =>
        {
            _status.Text = IsRussian ? "Проверяю файл…" : "Checking file…";

            string ffprobePath = ToolLocator.Find("ffprobe");
            var probeService = new FfprobeService(ffprobePath);
            _probeResult = await probeService.ProbeAsync(_selectedFile);

            string ffmpegPath = ToolLocator.Find("ffmpeg");
            var converter = new MediaConverter(ffmpegPath);
            var formats = converter.GetAllowedFormats(_probeResult);

            if (formats.Count == 0)
            {
                _status.Text = IsRussian
                    ? "Не найдены поддерживаемые аудио- или видеопотоки."
                    : "No supported audio or video streams were found.";
                return;
            }

            _formatBox.Items.AddRange(formats.Cast<object>().ToArray());
            _formatBox.SelectedIndex = 0;
            _formatBox.Enabled = true;
            _convertButton.Enabled = true;
            _status.Text = IsRussian
                ? "Файл проверен. Выбери формат и нажми «Конвертировать»."
                : "File checked. Choose a format and click “Convert”.";
        });
    }

    private async Task ConvertAsync()
    {
        if (_selectedFile is null ||
            _probeResult is null ||
            _formatBox.SelectedItem is not string extension)
        {
            return;
        }

        string output = Path.Combine(
            Path.GetDirectoryName(_selectedFile)!,
            $"{Path.GetFileNameWithoutExtension(_selectedFile)}_converted.{extension}");

        if (File.Exists(output))
        {
            MessageBox.Show(
                this,
                IsRussian
                    ? $"Файл уже существует:\n{output}"
                    : $"The output file already exists:\n{output}",
                "UniDocVerter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _progressBar.Value = 0;
        _progressBar.Visible = true;
        _cancelButton.Visible = true;

        bool hasMeasurableDuration =
            _probeResult.Duration is { TotalMilliseconds: > 0 } &&
            extension is not ("png" or "jpg" or "webp");

        _progressBar.Style = hasMeasurableDuration
            ? ProgressBarStyle.Continuous
            : ProgressBarStyle.Marquee;

        await RunBusyOperationAsync(async () =>
        {
            _conversionCts = new CancellationTokenSource();
            _cancelButton.Enabled = true;
            _status.Text = IsRussian ? "Конвертирую…" : "Converting…";

            try
            {
                var converter = new MediaConverter(ToolLocator.Find("ffmpeg"));

                var progress = new Progress<int>(value =>
                {
                    if (_progressBar.Style == ProgressBarStyle.Continuous)
                        _progressBar.Value = Math.Clamp(value, 0, 100);
                });

                await converter.ConvertAsync(
                    _selectedFile,
                    output,
                    extension,
                    _probeResult,
                    progress,
                    _conversionCts.Token);

                _status.Text = IsRussian
                    ? $"Готово: {output}"
                    : $"Done: {output}";

                var result = MessageBox.Show(
                    this,
                    IsRussian
                        ? "Конвертация завершена. Открыть папку с результатом?"
                        : "Conversion finished. Open the output folder?",
                    "UniDocVerter",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{output}\"",
                        UseShellExecute = true
                    });
                }
            }
            finally
            {
                _conversionCts?.Dispose();
                _conversionCts = null;
                _cancelButton.Enabled = false;
                _cancelButton.Visible = false;
                _progressBar.Visible = false;
            }
        });
    }
    private readonly ProgressBar _progressBar = new()
    {
        Minimum = 0,
        Maximum = 100,
        Value = 0
    };

    private readonly Button _cancelButton = new()
    {
        Enabled = false
    };

    private CancellationTokenSource? _conversionCts;
    private async Task RunBusyOperationAsync(Func<Task> operation)
    {
        if (_isBusy)
            return;

        _isBusy = true;
        _chooseButton.Enabled = false;
        _convertButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            _status.Text = IsRussian ? "Конвертация отменена." : "Conversion cancelled.";
        }
        catch (Win32Exception ex)
        {
            _status.Text = ex.Message;
            MessageBox.Show(
                this,
                IsRussian
                    ? $"Не найден FFmpeg или FFprobe:\n{ex.Message}"
                    : $"FFmpeg or FFprobe was not found:\n{ex.Message}",
                "UniDocVerter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            _status.Text = IsRussian ? "Операция не выполнена." : "Operation failed.";
            MessageBox.Show(
                this,
                ex.Message,
                IsRussian ? "Ошибка" : "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _chooseButton.Enabled = true;
            _convertButton.Enabled = _selectedFile is not null &&
                                     _probeResult is not null &&
                                     _formatBox.SelectedIndex >= 0;
            _isBusy = false;
        }
    }

    private static void StyleButton(Button button, bool secondary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = secondary ? 1 : 0;
        button.FlatAppearance.BorderColor = Color.FromArgb(68, 79, 96);
        button.BackColor = secondary
            ? Color.FromArgb(38, 45, 58)
            : Color.FromArgb(70, 115, 235);
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
    }
}