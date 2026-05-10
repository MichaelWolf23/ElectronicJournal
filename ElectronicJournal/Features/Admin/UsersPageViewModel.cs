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

public partial class UsersPageViewModel : PageViewModelBase
{
    private readonly UserRepository userRepository;

    [ObservableProperty]
    private ObservableCollection<UserListItem> users = new();

    [ObservableProperty]
    private ObservableCollection<Role> roles = new();

    [ObservableProperty]
    private UserListItem? selectedUser;

    [ObservableProperty]
    private int selectedRoleId;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private string resultMessage = "Выберите пользователя или создайте нового.";

    [ObservableProperty]
    private string formTitle = "Новый пользователь";

    [ObservableProperty]
    private int userCount;

    [ObservableProperty]
    private int activeUserCount;

    [ObservableProperty]
    private int inactiveUserCount;

    public UsersPageViewModel(UserRepository userRepository)
        : base("Пользователи")
    {
        this.userRepository = userRepository;
        Load();
    }

    partial void OnSelectedUserChanged(UserListItem? value)
    {
        if (value is null)
        {
            return;
        }

        FormTitle = "Редактирование пользователя";
        SelectedRoleId = value.RoleId;
        Username = value.Username;
        FullName = value.FullName;
        Email = value.Email ?? string.Empty;
        Phone = value.Phone ?? string.Empty;
        Password = string.Empty;
        IsActive = value.IsActive;
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            Roles = new ObservableCollection<Role>(userRepository.GetRoles());
            Users = new ObservableCollection<UserListItem>(userRepository.GetUsers());
            UserCount = Users.Count;
            ActiveUserCount = Users.Count(user => user.IsActive);
            InactiveUserCount = Users.Count - ActiveUserCount;
            SelectedRoleId = Roles.FirstOrDefault()?.RoleId ?? 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить пользователей: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedRoleId == 0)
        {
            ResultMessage = "Выберите роль.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(FullName))
        {
            ResultMessage = "Заполните логин и ФИО.";
            return;
        }

        if (SelectedUser is null && string.IsNullOrWhiteSpace(Password))
        {
            ResultMessage = "Для нового пользователя нужен пароль.";
            return;
        }

        try
        {
            if (SelectedUser is null)
            {
                userRepository.CreateUser(new User(
                    0,
                    SelectedRoleId,
                    Username.Trim(),
                    PasswordHasher.Hash(Password),
                    FullName.Trim(),
                    NullIfWhiteSpace(Email),
                    NullIfWhiteSpace(Phone),
                    IsActive,
                    string.Empty,
                    null));
                ResultMessage = "Пользователь создан.";
            }
            else
            {
                userRepository.UpdateUser(new User(
                    SelectedUser.UserId,
                    SelectedRoleId,
                    Username.Trim(),
                    string.Empty,
                    FullName.Trim(),
                    NullIfWhiteSpace(Email),
                    NullIfWhiteSpace(Phone),
                    IsActive,
                    SelectedUser.CreatedAt,
                    null));

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    userRepository.UpdatePassword(SelectedUser.UserId, PasswordHasher.Hash(Password));
                }

                ResultMessage = "Пользователь обновлен.";
            }

            ClearForm();
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить пользователя: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedUser = null;
        FormTitle = "Новый пользователь";
        SelectedRoleId = Roles.FirstOrDefault()?.RoleId ?? 0;
        Username = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Password = string.Empty;
        IsActive = true;
    }

    [RelayCommand]
    private void ToggleActive()
    {
        if (SelectedUser is null)
        {
            ResultMessage = "Сначала выберите пользователя.";
            return;
        }

        IsActive = !IsActive;
        Save();
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
