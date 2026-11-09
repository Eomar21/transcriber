using System.ComponentModel;

namespace Transcriber.Maui.Controls;

/// <summary>Scrolls a page so that the progress bar is in view when a run starts.</summary>
internal static class ScrollToProgress
{
    /// <param name="status">The view model of the progress card. It must raise a change for <c>ShowProgress</c>.</param>
    /// <param name="showProgress">Tells whether the card is visible now.</param>
    public static void When(INotifyPropertyChanged status, Func<bool> showProgress, ScrollView scroller, View card)
    {
        status.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName != "ShowProgress" || !showProgress()) return;

            // The card was hidden until now. Scroll only when the page has made room for it. Before that, the page is
            // not long enough to scroll as far as the card.
            for (var wait = 0; wait < 30 && !HasRoom(scroller, card); wait++) await Task.Delay(50);

            try { await scroller.ScrollToAsync(card, ScrollToPosition.Center, animated: true); }
            catch (Exception) { /* a page that is not shown cannot scroll */ }
        };
    }

    private static bool HasRoom(ScrollView scroller, View card) =>
        card.Height > 0 && card.Y > 0 && scroller.ContentSize.Height >= card.Y + card.Height;
}
