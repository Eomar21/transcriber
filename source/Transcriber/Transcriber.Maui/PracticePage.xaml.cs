using System.ComponentModel;
using Transcriber.ViewModels;

namespace Transcriber.Maui;

public partial class PracticePage : ContentPage
{
    private const double MaxColumnWidth = 912;
    private const double SideMargin = 24;

    // Below this width the tiles go three to a row instead of five.
    private const double WideLayoutWidth = 760;

    private readonly PracticeViewModel _viewModel;
    private int _tileColumns;

    // The page is only a view: all behaviour lives in the view model.
    public PracticePage(PracticeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PracticeViewModel.Metrics)) BuildMetricTiles();
    }

    // Look for Ollama each time the tab is shown, so starting it later is picked up without restarting the app.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshFeedbackModelsCommand.Execute(null);
    }

    // Layout only: keep the content column centred and no wider than 912, and reflow the metric tiles.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;

        var columnWidth = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
        Column.WidthRequest = columnWidth;

        var columns = columnWidth >= WideLayoutWidth ? 5 : 3;
        if (columns != _tileColumns) BuildMetricTiles(columns);
    }

    private void BuildMetricTiles(int? columns = null)
    {
        _tileColumns = columns ?? (_tileColumns == 0 ? 5 : _tileColumns);

        MetricsGrid.Children.Clear();
        MetricsGrid.ColumnDefinitions.Clear();
        MetricsGrid.RowDefinitions.Clear();

        for (var c = 0; c < _tileColumns; c++)
            MetricsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var metrics = _viewModel.Metrics;
        for (var i = 0; i < metrics.Count; i++)
        {
            if (i % _tileColumns == 0) MetricsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            MetricsGrid.Add(CreateTile(metrics[i]), i % _tileColumns, i / _tileColumns);
        }
    }

    private static View CreateTile(MetricItem metric)
    {
        var resources = Application.Current!.Resources;
        var tile = new Border
        {
            Style = (Style)resources["Card"],
            Padding = new Thickness(16, 12),
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label { Text = metric.Label, Style = (Style)resources["Caption"] },
                    new Label { Text = metric.Value, FontSize = 22, FontAttributes = FontAttributes.Bold, Style = (Style)resources["Body"] },
                    new Label { Text = metric.Hint, Style = (Style)resources["Caption"], LineBreakMode = LineBreakMode.WordWrap },
                },
            },
        };
        SemanticProperties.SetDescription(tile, $"{metric.Label}: {metric.Value}. {metric.Hint}");
        return tile;
    }
}
