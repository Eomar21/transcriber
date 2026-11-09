namespace Transcriber.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // "Open with" and tests: files passed on the command line are added to the list.
            var files = Environment.GetCommandLineArgs().Skip(1).Where(File.Exists).ToList();
            if (files.Count > 0)
                IPlatformApplication.Current?.Services.GetService<Transcriber.ViewModels.Transcribe.TranscribeViewModel>()?.Files.AddFiles(files);

            return new Window(new AppShell())
            {
                Title = "Transcriber",
                Width = 1040,
                Height = 820,
                MinimumWidth = 520,
                MinimumHeight = 560,
            };
        }
    }
}
