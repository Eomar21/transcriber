namespace Transcriber.Services.Models;

public enum LaunchResult
{
    Opened,

    /// <summary>The file or its folder no longer exists.</summary>
    NotFound,

    /// <summary>No application is registered for this file type.</summary>
    NoHandler,

    Failed,
}
