namespace Transcriber.Services.Transcription;

/// <summary>
/// Keeps the model of the last run in memory, so the next run with the same model starts without a load. A model that
/// is not used for some time is released, because a model holds much memory (on the graphics card too).
/// One model is kept at a time. The caller takes the model for a run and gives it back when the run ends.
/// </summary>
/// <typeparam name="TKey">What identifies a loaded model, for example its file and where it runs.</typeparam>
internal sealed class LoadedModelCache<TKey, TModel> : IDisposable
    where TKey : notnull
    where TModel : class, IDisposable
{
    private readonly object _gate = new();
    private readonly Func<TKey, TModel> _load;
    private readonly TimeSpan _keepFor;
    private readonly Timer _timer;
    private TKey? _key;
    private TModel? _model;
    private bool _inUse;

    /// <param name="load">Loads a model. It is called only when the model is not in memory.</param>
    /// <param name="keepFor">How long an unused model stays in memory. Zero releases the model when it is given back.</param>
    public LoadedModelCache(Func<TKey, TModel> load, TimeSpan keepFor)
    {
        _load = load;
        _keepFor = keepFor;
        _timer = new Timer(_ => ReleaseIfUnused(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Gives the model for the key. It loads the model when another model, or no model, is in memory.
    /// Call <see cref="Return"/> when the run ends.
    /// </summary>
    /// <exception cref="InvalidOperationException">A model is taken and was not given back.</exception>
    public TModel Take(TKey key)
    {
        lock (_gate)
        {
            if (_inUse) throw new InvalidOperationException("The model is in use by another run.");
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            if (_model is null || !EqualityComparer<TKey>.Default.Equals(_key, key))
            {
                // Release the old model first, so two models are never in memory at the same time.
                Release();
                _model = _load(key);
                _key = key;
            }

            _inUse = true;
            return _model;
        }
    }

    /// <summary>The run ended. The model stays in memory for the next run, until it is unused for too long.</summary>
    public void Return()
    {
        lock (_gate)
        {
            if (!_inUse) return;
            _inUse = false;
            if (_keepFor <= TimeSpan.Zero) Release();
            else _timer.Change(_keepFor, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _timer.Dispose();
            Release();
        }
    }

    private void ReleaseIfUnused()
    {
        lock (_gate)
        {
            // A run can start in the moment that the timer fires. The model of that run must stay.
            if (!_inUse) Release();
        }
    }

    private void Release()
    {
        _model?.Dispose();
        _model = null;
        _key = default;
    }
}
