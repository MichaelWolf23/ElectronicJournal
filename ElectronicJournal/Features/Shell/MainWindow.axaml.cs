using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ElectronicJournal.ViewModels;

namespace ElectronicJournal.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.LogoutRequested += Logout;
            }
        }

        private void Logout()
        {
            var loginWindow = new LoginWindow();
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = loginWindow;
            }

            loginWindow.Show();
            Close();
        }
    }
}
