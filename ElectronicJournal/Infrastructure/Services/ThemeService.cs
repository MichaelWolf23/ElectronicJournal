using Avalonia;
using Avalonia.Styling;

namespace ElectronicJournal.Services;

public static class ThemeService
{
    public static bool IsDarkTheme =>
        Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;

    public static void SetTheme(bool useDarkTheme)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = useDarkTheme
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }

    public static bool ToggleTheme()
    {
        var useDarkTheme = !IsDarkTheme;
        SetTheme(useDarkTheme);
        return useDarkTheme;
    }
}
