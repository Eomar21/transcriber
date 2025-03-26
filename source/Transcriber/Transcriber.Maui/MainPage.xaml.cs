using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Transcriber.Maui
{
    public partial class MainPage : ContentPage
    {
        private readonly IFileReaderService fileReaderService;
        private readonly IWhisperService whisperService;

        public MainPage(IFileReaderService fileReaderService, IWhisperService whisperService)
        {
            InitializeComponent();
            this.fileReaderService = fileReaderService;
            this.whisperService = whisperService;
        }

        // This method opens a file picker to let the user choose an mp3 or mp4 file.
        private async void OnBrowseClicked(object sender, EventArgs e)
        {
            try
            {
                // Clear old location
                TranscriptLocationLabel.Text = string.Empty;

                // Define file types for Windows and macOS
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".mp3", ".mp4", ".wav" } },
                    { DevicePlatform.macOS, new[] { ".mp3", ".mp4", ".wav" } },
                });

                var options = new PickOptions
                {
                    PickerTitle = "Select an mp3 or mp4 file",
                    FileTypes = customFileType,
                };

                // Open the file picker
                var result = await FilePicker.PickAsync(options);
                if (result != null)
                {
                    // Display the selected file path in the Entry control
                    FilePathEntry.Text = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // This method shows a spinning wheel for 3 seconds to simulate file processing,
        // then updates the label with the transcript file location.
        private async void OnProcessClicked(object sender, EventArgs e)
        {
            // Start the spinning wheel
            ProcessingIndicator.IsVisible = true;
            ProcessingIndicator.IsRunning = true;

            // Simulate processing delay (3 seconds)
            var path = await fileReaderService.GetAudioFileFromFileAsync(FilePathEntry.Text);
            var output = await whisperService.TranscribeAsync(path);

            // Stop the spinning wheel
            ProcessingIndicator.IsRunning = false;
            ProcessingIndicator.IsVisible = false;

            // Update the label with the transcript location
            if (output.Any())
            {
                TranscriptLocationLabel.Text = $"Transcript saved here: {Path.GetFileNameWithoutExtension(FilePathEntry.Text)}_transcript.txt";
            }

        }
    }
}
