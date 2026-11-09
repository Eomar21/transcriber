using Transcriber.ViewModels;
using Transcriber.ViewModels.Practice;

/// <summary>Builds the Practice tab the way the app does: the sections first, then the tab that connects them.</summary>
internal static class PracticeFixture
{
    public static PracticeViewModel Create(
        IFileReaderService reader,
        IWhisperService whisper,
        IFilePickerService picker,
        Transcriber.Coach.Interfaces.IPracticeHistory history,
        Transcriber.Coach.Interfaces.ICoachFeedback feedback,
        IAudioRecorder recorder,
        Func<DateTimeOffset>? now = null,
        string? recordingsDirectory = null)
    {
        var state = new PracticeState();
        var analysis = new AnalysisViewModel();
        var microphone = new MicrophoneViewModel(recorder, state, now, recordingsDirectory);
        return new PracticeViewModel(
            state,
            new SourceViewModel(picker, ServicesFixture.Languages, microphone, state),
            new PracticeStatusViewModel(state),
            analysis,
            new FeedbackViewModel(feedback, analysis, state),
            new HistoryViewModel(history),
            reader, whisper, ServicesFixture.Models, CoachFixture.Analyzer, CoachFixture.Advisor, CoachFixture.VerbatimPrompts, now);
    }
}
