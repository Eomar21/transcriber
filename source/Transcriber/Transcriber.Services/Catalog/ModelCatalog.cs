using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Catalog;

/// <summary>The model list and the model roles from the app settings.</summary>
internal sealed class ModelCatalog : IModelCatalog
{
    private readonly Dictionary<string, WhisperModel> _byId;

    public ModelCatalog(IOptions<ModelSettings> options)
    {
        var settings = options.Value;
        All = [.. settings.Catalog.Select(m => m.ToModel())];
        _byId = All.ToDictionary(m => m.Id, StringComparer.Ordinal);

        // The settings are checked when the app starts, so these ids exist.
        Default = _byId[settings.DefaultModelId];
        Suggested = _byId[settings.SuggestedModelId];
        Practice = _byId[settings.PracticeModelId];
    }

    public IReadOnlyList<WhisperModel> All { get; }

    public WhisperModel Default { get; }

    public WhisperModel Suggested { get; }

    public WhisperModel Practice { get; }

    public WhisperModel? Find(string id) => _byId.GetValueOrDefault(id);
}
