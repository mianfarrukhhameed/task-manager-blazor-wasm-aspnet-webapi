using MudBlazor;

namespace Fistix.TaskManager.WebApp.Shared.Theme;

public static class AppTheme
{
    public static MudTheme Light { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#3949ab",
            Secondary = "#00897b",
            AppbarBackground = "#3949ab",
            DrawerBackground = "#ffffff",
            DrawerText = "#424242",
            Background = "#f5f5f5",
            Surface = "#ffffff"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };

    public static MudTheme Dark { get; } = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#7986cb",
            Secondary = "#4db6ac",
            AppbarBackground = "#1e1e2f",
            DrawerBackground = "#1a1a27",
            Background = "#121212",
            Surface = "#1e1e2f"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };
}
