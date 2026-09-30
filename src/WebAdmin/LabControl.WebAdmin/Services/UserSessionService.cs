using Microsoft.JSInterop;
using System.Text.Json;

namespace LabControl.WebAdmin.Services;

public class UserSession
{
    public string Token { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NombreCompleto { get; set; } = default!;
    public string Rol { get; set; } = default!;
    public string? FotoUrl { get; set; }
}

public class UserSessionService
{
    public UserSession? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public bool IsInitialized { get; private set; }

    public event Action? OnChange;

    public async Task LoginAsync(IJSRuntime js, UserSession session, bool rememberMe = true)
    {
        CurrentUser = session;
        IsInitialized = true;
        try
        {
            var json = JsonSerializer.Serialize(session);
            await js.InvokeVoidAsync("authManager.saveSession", json, rememberMe);
        }
        catch { }
        NotifyStateChanged();
    }

    public void Login(UserSession session)
    {
        CurrentUser = session;
        IsInitialized = true;
        NotifyStateChanged();
    }

    public async Task<bool> TryRestoreSessionAsync(IJSRuntime js)
    {
        if (IsAuthenticated)
        {
            IsInitialized = true;
            return true;
        }

        try
        {
            var json = await js.InvokeAsync<string?>("authManager.getSession");
            if (!string.IsNullOrWhiteSpace(json))
            {
                var session = JsonSerializer.Deserialize<UserSession>(json);
                if (session != null && !string.IsNullOrWhiteSpace(session.Email))
                {
                    CurrentUser = session;
                    IsInitialized = true;
                    NotifyStateChanged();
                    return true;
                }
            }
        }
        catch { }

        IsInitialized = true;
        return false;
    }

    public async Task LogoutAsync(IJSRuntime js)
    {
        CurrentUser = null;
        try
        {
            await js.InvokeVoidAsync("authManager.clearSession");
        }
        catch { }
        NotifyStateChanged();
    }

    public void UpdateProfile(string nombreCompleto, string? fotoUrl = null)
    {
        if (CurrentUser != null)
        {
            CurrentUser.NombreCompleto = nombreCompleto;
            CurrentUser.FotoUrl = string.IsNullOrWhiteSpace(fotoUrl) ? null : fotoUrl;
            NotifyStateChanged();
        }
    }

    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

