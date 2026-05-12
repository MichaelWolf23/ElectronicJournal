using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class LoginWindowViewModel : ViewModelBase
{
    private readonly AuthService authService;
    private readonly UserRepository userRepository;
    private bool isDatabaseAvailable = true;

    [ObservableProperty]
    private ObservableCollection<Role> roles = new();

    [ObservableProperty]
    private int selectedRoleId;

    [ObservableProperty]
    private string loginUsername = string.Empty;

    [ObservableProperty]
    private string loginPassword = string.Empty;

    [ObservableProperty]
    private string registerUsername = string.Empty;

    [ObservableProperty]
    private string registerPassword = string.Empty;

    [ObservableProperty]
    private string registerPasswordRepeat = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private string message = "Войдите в систему или зарегистрируйте нового пользователя.";

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private string themeButtonText = "Темная тема";

    [ObservableProperty]
    private bool canUseDatabase = true;

    public event Action<AuthenticatedUser>? LoginSucceeded;

    public LoginWindowViewModel(AuthService authService, UserRepository userRepository)
    {
        this.authService = authService;
        this.userRepository = userRepository;
        IsDarkTheme = ThemeService.IsDarkTheme;
        ThemeButtonText = IsDarkTheme ? "Светлая тема" : "Темная тема";

        LoadRoles();
    }

    [RelayCommand]
    private void Login()
    {
        if (!isDatabaseAvailable)
        {
            Message = "База данных не найдена или не содержит нужных таблиц. Вход временно недоступен.";
            return;
        }

        if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            Message = "Введите логин и пароль.";
            return;
        }

        try
        {
            var user = authService.Login(LoginUsername, LoginPassword);
            if (user is null)
            {
                Message = "Неверный логин или пароль, либо пользователь отключен.";
                return;
            }

            LoginSucceeded?.Invoke(user);
        }
        catch (Exception ex)
        {
            Message = $"Не удалось выполнить вход: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void Register()
    {
        if (!isDatabaseAvailable)
        {
            Message = "База данных не найдена или не содержит нужных таблиц. Регистрация временно недоступна.";
            return;
        }

        if (SelectedRoleId == 0)
        {
            Message = "Выберите роль пользователя.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterUsername) ||
            string.IsNullOrWhiteSpace(RegisterPassword) ||
            string.IsNullOrWhiteSpace(FullName))
        {
            Message = "Заполните логин, пароль и ФИО.";
            return;
        }

        if (RegisterPassword.Length < 4)
        {
            Message = "Пароль должен быть не короче 4 символов.";
            return;
        }

        if (RegisterPassword != RegisterPasswordRepeat)
        {
            Message = "Пароли не совпадают.";
            return;
        }

        try
        {
            var user = authService.Register(
                SelectedRoleId,
                RegisterUsername,
                RegisterPassword,
                FullName,
                Email,
                Phone);

            LoginSucceeded?.Invoke(user);
        }
        catch (Exception ex)
        {
            Message = $"Не удалось зарегистрировать пользователя: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = ThemeService.ToggleTheme();
        ThemeButtonText = IsDarkTheme ? "Светлая тема" : "Темная тема";
    }

    private void LoadRoles()
    {
        try
        {
            Roles = new ObservableCollection<Role>(userRepository.GetRoles());
            SelectedRoleId = Roles.FirstOrDefault()?.RoleId ?? 0;
            isDatabaseAvailable = Roles.Count > 0;
            CanUseDatabase = isDatabaseAvailable;
            Message = isDatabaseAvailable
                ? "Войдите в систему или зарегистрируйте нового пользователя."
                : "В базе данных нет ролей. Проверьте структуру electronic_journal.db.";
        }
        catch (Exception ex)
        {
            isDatabaseAvailable = false;
            CanUseDatabase = false;
            Roles = new ObservableCollection<Role>();
            SelectedRoleId = 0;
            Message = $"База данных недоступна: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }
}
