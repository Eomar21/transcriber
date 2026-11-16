using System.ComponentModel;

namespace Transcriber.Maui.Controls;

/// <summary>Scrolls a page so that a card comes into view when something happens: a run starts, or a run ends.</summary>
internal static class ScrollIntoView
{
    /// <summary>Scrolls when a property of <paramref name="source"/> changes and <paramref name="condition"/> is true.</summary>
    public static void When(INotifyPropertyChanged source, string property, Func<bool> condition, ScrollView scroller, View card,
        ScrollToPosition position = ScrollToPosition.Center)
    {
        source.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == property && condition()) await ToAsync(scroller, card, position);
        };
    }

    /// <summary>Scrolls to the card as soon as the page has made room for it.</summary>
    public static async Task ToAsync(ScrollView scroller, View card, ScrollToPosition position = ScrollToPosition.Center)
    {
        // The card was hidden until now. Scroll only when the page has made room for it. Before that, the page is
        // not long enough to scroll as far as the card.
        for (var wait = 0; wait < 30 && !HasRoom(scroller, card); wait++) await Task.Delay(50);

        try { await scroller.ScrollToAsync(card, position, animated: true); }
        catch (Exception) { /* a page that is not shown cannot scroll */ }
    }

    private static bool HasRoom(ScrollView scroller, View card) =>
        card.Height > 0 && card.Y > 0 && scroller.ContentSize.Height >= card.Y + card.Height;
}
