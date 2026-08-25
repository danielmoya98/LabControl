namespace LabControl.WebAdmin.Services;

public class UserSession
{
    public string Token { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NombreCompleto { get; set; } = default!;
    public string Rol { get; set; } = default!;
}

public class UserSessionService
{
    public UserSession? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public event Action? OnChange;

    public void Login(UserSession session)
    {
        CurrentUser = session;
        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
