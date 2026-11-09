namespace Transcriber.ViewModels.Progress;

/// <summary>Picks a random look, and never the same look twice in a row.</summary>
public sealed class ProgressLookPicker : IProgressLookPicker
{
    private readonly IReadOnlyList<ProgressLook> _looks;
    private readonly Random _random;
    private readonly object _gate = new();
    private int _last = -1;

    public ProgressLookPicker(IReadOnlyList<ProgressLook> looks, Random? random = null)
    {
        if (looks.Count == 0) throw new ArgumentException("There must be at least one look.", nameof(looks));
        _looks = looks;
        _random = random ?? new Random();
    }

    public ProgressLook Pick()
    {
        lock (_gate)
        {
            var next = _looks.Count == 1 ? 0 : _random.Next(_looks.Count - 1);
            // The numbers 0 to Count-2 skip the last look. Shift the ones from the last look upward.
            if (_looks.Count > 1 && next >= _last && _last >= 0) next++;
            _last = next;
            return _looks[next];
        }
    }
}
