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
