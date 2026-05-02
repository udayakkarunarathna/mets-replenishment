using METS.Blazor.Models;

namespace METS.Blazor.Services;

/// <summary>
/// Lightweight in-memory "session" — tracks the currently active user.
/// In a real app this would be replaced by proper authentication.
/// </summary>
public class UserSession
{
    public UserDto? CurrentUser { get; private set; }

    public bool IsWorker   => CurrentUser?.Role == "Worker";
    public bool IsReviewer => CurrentUser?.Role == "Reviewer";
    public bool IsLoggedIn => CurrentUser is not null;

    public event Action? OnChange;

    public void SetUser(UserDto user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }
}
