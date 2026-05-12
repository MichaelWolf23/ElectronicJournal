using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace ElectronicJournal.Services;

public static class ConfirmationDialogService
{
    public static async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 380,
            Height = 190,
            MinWidth = 360,
            MinHeight = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = BuildContent(message)
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } owner)
        {
            return await dialog.ShowDialog<bool>(owner);
        }

        dialog.Show();
        return false;
    }

    private static Control BuildContent(string message)
    {
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };

        var cancelButton = new Button
        {
            Content = "Отмена",
            MinWidth = 92
        };

        var deleteButton = new Button
        {
            Content = "Удалить",
            MinWidth = 92,
            Foreground = Brushes.White,
            Background = Brushes.Firebrick
        };

        cancelButton.Click += (_, _) => CloseWindow(cancelButton, false);
        deleteButton.Click += (_, _) => CloseWindow(deleteButton, true);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { cancelButton, deleteButton }
        };

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            Margin = new Thickness(20),
            RowSpacing = 18
        };
        Grid.SetRow(buttons, 1);
        grid.Children.Add(text);
        grid.Children.Add(buttons);

        return grid;
    }

    private static void CloseWindow(Control control, bool result)
    {
        if (TopLevel.GetTopLevel(control) is Window window)
        {
            window.Close(result);
        }
    }
}
