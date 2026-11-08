using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

/// <summary>Native confirmation dialog on Windows and macOS.</summary>
public class MauiDialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page is not null && await page.DisplayAlert(title, message, accept, cancel);
    }
}
