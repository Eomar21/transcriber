namespace Transcriber.Maui.Controls;

/// <summary>
/// The scrolling body of a screen: its content is a column that is centred, at most <see cref="MaxColumnWidth"/> wide,
/// and narrower than the window by the side margin. Every screen uses it, so all screens line up in the same way.
/// </summary>
public class PageColumn : ScrollView
{
    public const double MaxColumnWidth = 912;
    public const double SideMargin = 24;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0 && Content is View column)
        {
            column.HorizontalOptions = LayoutOptions.Center;
            column.WidthRequest = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
        }
    }
}
