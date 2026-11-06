public class ModelBundlingTests : IDisposable
{
    private readonly string _userDir = Directory.CreateTempSubdirectory().FullName;
    private readonly string _bundledDir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose()
    {
        Directory.Delete(_userDir, true);
        Directory.Delete(_bundledDir, true);
    }

    [Fact]
    public async Task A_model_shipped_with_the_app_is_used_without_any_download()
    {
        var bundled = Path.Combine(_bundledDir, WhisperModel.Small.FileName);
        File.WriteAllText(bundled, "model");
        var manager = new ModelManager(_userDir, _bundledDir);

        Assert.True(manager.IsBundled(WhisperModel.Small));
        Assert.True(manager.IsDownloaded(WhisperModel.Small));
        Assert.Equal(bundled, await manager.EnsureModelAsync(WhisperModel.Small));
        Assert.Empty(Directory.GetFiles(_userDir));
    }

    [Fact]
    public async Task The_bundled_copy_wins_over_a_previously_downloaded_one()
    {
        var bundled = Path.Combine(_bundledDir, WhisperModel.Small.FileName);
        File.WriteAllText(bundled, "bundled");
        File.WriteAllText(Path.Combine(_userDir, WhisperModel.Small.FileName), "downloaded");

        Assert.Equal(bundled, await new ModelManager(_userDir, _bundledDir).EnsureModelAsync(WhisperModel.Small));
    }

    [Fact]
    public void Other_models_are_not_reported_as_bundled()
    {
        File.WriteAllText(Path.Combine(_bundledDir, WhisperModel.Small.FileName), "model");
        var manager = new ModelManager(_userDir, _bundledDir);

        Assert.False(manager.IsBundled(WhisperModel.LargeV3TurboQ5));
        Assert.False(manager.IsDownloaded(WhisperModel.LargeV3TurboQ5));
    }

    [Fact]
    public void The_app_default_model_is_the_one_that_gets_bundled()
    {
        // The build step bundles ggml-small.bin; the default option must point at exactly that file.
        Assert.Equal("ggml-small.bin", new TranscriptionOptions().Model.FileName);
    }
}
