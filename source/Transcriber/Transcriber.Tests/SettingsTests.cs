/// <summary>The settings are data. These tests check the default file, the user file and the checks on the values.</summary>
public class SettingsTests
{
    private static string WriteUserFile(string json)
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, json);
        return file;
    }

    [Fact]
    public void The_default_settings_are_valid_and_complete()
    {
        var s = SettingsLoader.Load();

        Assert.Equal(16000, s.Audio.SampleRate);
        Assert.Equal(5, s.Models.Catalog.Count);
        Assert.Equal(12, s.Languages.Catalog.Count);
        Assert.Equal(30, s.Media.NativeExtensions.Count + s.Media.FfmpegAudioExtensions.Count + s.Media.FfmpegVideoExtensions.Count);
        Assert.NotEmpty(s.Transcription.Hallucinations);
    }

    [Fact]
    public void A_file_that_does_not_exist_is_ignored()
    {
        var s = SettingsLoader.Load(Path.Combine(Path.GetTempPath(), "no-such-settings.json"));

        Assert.Equal("small", s.Models.DefaultModelId);
    }

    [Fact]
    public void A_user_file_changes_only_the_values_that_it_names()
    {
        var file = WriteUserFile("""{ "SpeechPlanning": { "MaxChunkSeconds": 120 } }""");
        try
        {
            var s = SettingsLoader.Load(file);

            Assert.Equal(120, s.SpeechPlanning.MaxChunkSeconds);
            Assert.Equal(0.3, s.SpeechPlanning.PaddingSeconds);
            Assert.Equal(5, s.Models.Catalog.Count);
        }
        finally { File.Delete(file); }
    }

    [Theory]
    [InlineData("""{ "Audio": { "SampleRate": 0 } }""", "SampleRate")]
    [InlineData("""{ "Models": { "DefaultModelId": "does-not-exist" } }""", "DefaultModelId")]
    [InlineData("""{ "Transcription": { "NoSpeechThreshold": 2 } }""", "NoSpeechThreshold")]
    [InlineData("""{ "SpeechPlanning": { "MaxChunkSeconds": 0 } }""", "SpeechPlanning")]
    [InlineData("""{ "Speakers": { "MinorSpeakerShare": 1.5 } }""", "Speakers")]
    public void A_value_that_cannot_work_stops_the_app_with_a_message_that_names_it(string json, string expected)
    {
        var file = WriteUserFile(json);
        try
        {
            var ex = Assert.Throws<SettingsException>(() => SettingsLoader.Load(file));

            Assert.Contains(expected, ex.Message);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void A_file_that_is_not_json_gives_a_settings_error()
    {
        var file = WriteUserFile("this is not json");
        try
        {
            Assert.Throws<SettingsException>(() => SettingsLoader.Load(file));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void Every_media_extension_is_known_by_the_format_catalog()
    {
        var formats = ServicesFixture.Formats;

        Assert.True(formats.IsSupported("talk.MP3"));
        Assert.True(formats.IsSupported("clip.mkv"));
        Assert.False(formats.IsSupported("notes.txt"));
        Assert.Equal(30, formats.SupportedExtensions.Count);
    }
}
