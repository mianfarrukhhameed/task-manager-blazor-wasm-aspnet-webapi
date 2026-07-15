using Microsoft.JSInterop;
using MudBlazor;

namespace Fistix.TaskManager.WebApp.Shared.Theme;

public sealed class ThemeService
{
    private const string StorageKey = "tm-theme-dark";
    private readonly IJSRuntime _js;

    public ThemeService(IJSRuntime js) => _js = js;

    public bool IsDarkMode { get; private set; }

    public MudTheme CurrentTheme => IsDarkMode ? AppTheme.Dark : AppTheme.Light;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            IsDarkMode = string.Equals(stored, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            IsDarkMode = false;
        }
    }

    public async Task SetDarkModeAsync(bool isDark)
    {
        IsDarkMode = isDark;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, isDark ? "true" : "false");
        }
        catch
        {
            // ignore persistence failures
        }

        Changed?.Invoke();
    }

    public Task ToggleAsync() => SetDarkModeAsync(!IsDarkMode);
}
