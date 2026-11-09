namespace Transcriber.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        /// <summary>The wanted size, or less when the screen is smaller. The margin leaves room for the title bar and the task bar. All sizes are in device-independent units.</summary>
        private static double FitToScreen(double wanted, double available, double margin) =>
            available <= 0 ? wanted : Math.Max(480, Math.Min(wanted, available - margin));

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // "Open with" and tests: files passed on the command line are added to the list.
            var files = Environment.GetCommandLineArgs().Skip(1).Where(File.Exists).ToList();
            if (files.Count > 0)
                IPlatformApplication.Current?.Services.GetService<Transcriber.ViewModels.Transcribe.TranscribeViewModel>()?.Files.AddFiles(files);

            // A window that is taller than the screen hides its lower part, and that part cannot be reached by scrolling.
            var display = DeviceDisplay.Current.MainDisplayInfo;

            var screenWidth = display.Width / display.Density;
            var screenHeight = display.Height / display.Density;
            var width = FitToScreen(1040, screenWidth, 40);
            var height = FitToScreen(820, screenHeight, 120);

            // The system puts a new window at an offset, and the lower part of a tall window can end below the screen.
            // The window opens in the middle of the screen instead, a little above the centre because of the task bar.
            return new Window(new AppShell())
            {
                Title = "Transcriber",
                Width = width,
                Height = height,
                X = Math.Max(0, (screenWidth - width) / 2),
                Y = Math.Max(0, (screenHeight - height) / 2 - 24),
                MinimumWidth = 520,
                MinimumHeight = 480,
            };
        }
    }
}
