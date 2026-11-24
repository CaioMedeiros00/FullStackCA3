using Microsoft.JSInterop;

namespace DriveNow.State;

/// <summary>Light/dark mode using Bootstrap's data-bs-theme attribute, remembered in localStorage.</summary>
public class ThemeService(IJSRuntime js)
{
    public bool IsDark { get; private set; }
    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        try
        {
            IsDark = await js.InvokeAsync<string>("driveNow.getTheme") == "dark";
            OnChange?.Invoke();
        }
        catch (JSException)
        {
            IsDark = false;
        }
    }

    public async Task ToggleAsync()
    {
        IsDark = !IsDark;
        await js.InvokeVoidAsync("driveNow.setTheme", IsDark ? "dark" : "light");
        OnChange?.Invoke();
    }
}
