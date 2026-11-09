using System.ComponentModel;
using Transcriber.ViewModels.Practice;

namespace Transcriber.Maui.Views.Practice;

/// <summary>The headline numbers as tiles. The tiles go five to a row when there is room and three to a row when there is not.</summary>
public partial class MetricsPanel : ContentView
{
    // Below this width the tiles go three to a row instead of five.
    private const double WideLayoutWidth = 760;

    private int _tileColumns;
    private AnalysisViewModel? _viewModel;

    public MetricsPanel() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = BindingContext as AnalysisViewModel;
        if (_viewModel is not null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalysisViewModel.Metrics)) BuildMetricTiles();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;

        var columns = width >= WideLayoutWidth ? 5 : 3;
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

        var metrics = _viewModel?.Metrics ?? [];
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
