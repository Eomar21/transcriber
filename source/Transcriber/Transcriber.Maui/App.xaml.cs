namespace Transcriber.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        /// <summary>The wanted size, or less when the screen is smaller. The margin leaves room for the title bar and the task bar.</summary>
        private static double FitToScreen(double wanted, double available, double margin, double density) =>
            (available <= 0 ? wanted : Math.Max(480, Math.Min(wanted, available - margin))) / WindowSizeScale(density);

        /// <summary>The Windows window takes its size in pixels, so on a scaled screen the size in DIP must be divided by the scale. The Mac window takes points.</summary>
        private static double WindowSizeScale(double density) =>
#if WINDOWS
            density > 0 ? density : 1;
#else
            1;
#endif

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // "Open with" and tests: files passed on the command line are added to the list.
            var files = Environment.GetCommandLineArgs().Skip(1).Where(File.Exists).ToList();
            if (files.Count > 0)
                IPlatformApplication.Current?.Services.GetService<Transcriber.ViewModels.Transcribe.TranscribeViewModel>()?.Files.AddFiles(files);

            // A window that is taller than the screen hides its lower part, and that part cannot be reached by scrolling.
            var display = DeviceDisplay.Current.MainDisplayInfo;

            return new Window(new AppShell())
            {
                Title = "Transcriber",
                Width = FitToScreen(1040, display.Width / display.Density, 40, display.Density),
                Height = FitToScreen(820, display.Height / display.Density, 120, display.Density),
                MinimumWidth = 520,
                MinimumHeight = 480,
            };
        }
    }
}
