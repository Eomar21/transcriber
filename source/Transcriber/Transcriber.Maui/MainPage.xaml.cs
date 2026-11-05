using System.Text;

namespace Transcriber.Maui
{
    public partial class MainPage : ContentPage
    {
        private sealed record SegmentRow(string Time, string Text);

        private readonly IFileReaderService fileReaderService;
        private readonly IWhisperService whisperService;

        private IReadOnlyList<FileResult> pickedFiles = [];
        private List<SegmentRow> allRows = [];
        private CancellationTokenSource? cts;

        public MainPage(IFileReaderService fileReaderService, IWhisperService whisperService)
        {
            InitializeComponent();
            this.fileReaderService = fileReaderService;
            this.whisperService = whisperService;

            LanguagePicker.ItemsSource = SupportedLanguages.All.Select(l => l.Name).ToList();
            LanguagePicker.SelectedIndex = 0;
            ModelPicker.ItemsSource = WhisperModel.All.Select(m => m.DisplayName).ToList();
            ModelPicker.SelectedIndex = Array.IndexOf(WhisperModel.All.ToArray(), WhisperModel.Small);
            FormatPicker.ItemsSource = new List<string> { "Text (.txt)", "Subtitles (.srt)", "Web subtitles (.vtt)", "Data (.json)" };
            FormatPicker.SelectedIndex = 0;
        }

        private async void OnBrowseClicked(object? sender, EventArgs e)
        {
            try
            {
                TranscriptLocationLabel.Text = string.Empty;
                var extensions = FileReaderService.SupportedExtensions.ToArray();
                var fileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, extensions },
                    { DevicePlatform.macOS, extensions.Select(x => x.TrimStart('.')).ToArray() },
                });

                var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Select audio or video files",
                    FileTypes = fileType,
                });

                var files = result?.ToList();
                if (files is { Count: > 0 })
                {
                    pickedFiles = files;
                    FilePathEntry.Text = files.Count == 1 ? files[0].FullPath : $"{files.Count} files selected";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnProcessClicked(object? sender, EventArgs e)
        {
            if (pickedFiles.Count == 0)
            {
                await DisplayAlertAsync("No file", "Choose one or more audio or video files first.", "OK");
                return;
            }

            var options = new TranscriptionOptions
            {
                Language = SupportedLanguages.All[LanguagePicker.SelectedIndex].Code,
                Model = WhisperModel.All[ModelPicker.SelectedIndex],
                Prompt = PromptEntry.Text,
                UseGpu = GpuSwitch.IsToggled,
            };
            var format = (TranscriptFormat)FormatPicker.SelectedIndex;

            SetBusy(true);
            cts = new CancellationTokenSource();
            var rows = new List<SegmentRow>();
            var saved = new List<string>();

            try
            {
                for (var i = 0; i < pickedFiles.Count; i++)
                {
                    var file = pickedFiles[i];
                    var prefix = pickedFiles.Count > 1 ? $"File {i + 1}/{pickedFiles.Count}: " : string.Empty;
                    var progress = new Progress<TranscriptionProgress>(p =>
                    {
                        StatusLabel.Text = prefix + p.Stage;
                        ProgressBar.Progress = Math.Clamp(p.Fraction, 0, 1);
                    });

                    StatusLabel.Text = prefix + "Preparing audio";
                    var audioPath = await fileReaderService.GetAudioFileFromFileAsync(file.FullPath);
                    try
                    {
                        var segments = await whisperService.TranscribeAsync(audioPath, options, progress, cts.Token);
                        saved.Add(await TranscriptExporter.WriteAsync(segments, file.FullPath, format));
                        rows.AddRange(segments.Select(s => new SegmentRow(
                            pickedFiles.Count > 1 ? $"{file.FileName} {s.Start:hh\\:mm\\:ss}" : $"{s.Start:hh\\:mm\\:ss}", s.Text)));
                    }
                    finally
                    {
                        // Converted audio is a temp file; never delete the user's own input.
                        if (!string.Equals(audioPath, file.FullPath, StringComparison.OrdinalIgnoreCase))
                            try { File.Delete(audioPath); } catch { /* best effort */ }
                    }
                }

                allRows = rows;
                SearchBox.Text = string.Empty;
                SegmentsView.ItemsSource = allRows;
                ResultsPanel.IsVisible = allRows.Count > 0;
                TranscriptLocationLabel.TextColor = Colors.Green;
                TranscriptLocationLabel.Text = saved.Count == 1
                    ? $"Transcript saved: {saved[0]}  (runtime: {whisperService.LastRuntime})"
                    : $"{saved.Count} transcripts saved next to the source files  (runtime: {whisperService.LastRuntime})";
            }
            catch (OperationCanceledException)
            {
                TranscriptLocationLabel.TextColor = Colors.Gray;
                TranscriptLocationLabel.Text = "Cancelled.";
            }
            catch (Exception ex)
            {
                TranscriptLocationLabel.TextColor = Colors.Red;
                TranscriptLocationLabel.Text = "Failed: " + ex.Message;
            }
            finally
            {
                cts.Dispose();
                cts = null;
                SetBusy(false);
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e) => cts?.Cancel();

        private void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            var q = e.NewTextValue?.Trim();
            SegmentsView.ItemsSource = string.IsNullOrEmpty(q)
                ? allRows
                : allRows.Where(r => r.Text.Contains(q, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        private async void OnCopyClicked(object? sender, EventArgs e)
        {
            var text = new StringBuilder();
            foreach (var row in allRows) text.AppendLine(row.Text.Trim());
            await Clipboard.Default.SetTextAsync(text.ToString());
        }

        private void SetBusy(bool busy)
        {
            ProcessButton.IsEnabled = !busy;
            BrowseButton.IsEnabled = !busy;
            CancelButton.IsVisible = busy;
            ProgressPanel.IsVisible = busy;
            if (busy) ProgressBar.Progress = 0;
        }
    }
}
