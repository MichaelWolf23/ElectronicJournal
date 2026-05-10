using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
using ElectronicJournal.ViewModels;

namespace ElectronicJournal.Views;

public partial class LoginWindow : Window
{
    private readonly DatabaseService databaseService = new();

    public LoginWindow()
    {
        InitializeComponent();

        var userRepository = new UserRepository(databaseService);
        var viewModel = new LoginWindowViewModel(
            new AuthService(userRepository),
            userRepository);
        viewModel.LoginSucceeded += OpenMainWindow;
        DataContext = viewModel;
    }

    private void OpenMainWindow(Models.Dto.AuthenticatedUser user)
    {
        var mainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel(databaseService, user)
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow;
        }

        mainWindow.Show();
        Close();
    }
}
