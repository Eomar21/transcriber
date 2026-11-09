using CommunityToolkit.Mvvm.ComponentModel;
using Transcriber.Coach.Models;

namespace Transcriber.ViewModels.Practice;

/// <summary>The result of an analysis: the numbers, the advice and the words that were heard.</summary>
public sealed partial class AnalysisViewModel : ObservableObject
{
    [ObservableProperty] private IReadOnlyList<MetricItem> _metrics = [];
    [ObservableProperty] private IReadOnlyList<TipItem> _tips = [];
    [ObservableProperty] private bool _hasResult;

    /// <summary>What was heard, so the numbers can be checked against the words.</summary>
    [ObservableProperty] private string _transcript = "";

    /// <summary>The report behind the numbers, or null when there is no result.</summary>
    public SpeechReport? Report { get; private set; }

    public void Show(SpeechReport report, string transcript, IReadOnlyList<MetricItem> metrics, IReadOnlyList<TipItem> tips)
    {
        Report = report;
        Transcript = transcript;
        Metrics = metrics;
        Tips = tips;
        HasResult = true;
    }

    public void Clear()
    {
        Report = null;
        Transcript = "";
        Metrics = [];
        Tips = [];
        HasResult = false;
    }
}
