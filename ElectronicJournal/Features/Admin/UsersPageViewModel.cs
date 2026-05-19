using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    private List<UserListItem> allUsers = new();

    [ObservableProperty]
    private ObservableCollection<UserListItem> users = new();

    [ObservableProperty]
    private ObservableCollection<UserListItem> registrationRequests = new();

    [ObservableProperty]
    private ObservableCollection<Role> roles = new();

    [ObservableProperty]
    private UserListItem? selectedUser;

    [ObservableProperty]
    private UserListItem? selectedRegistrationRequest;

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
    private string searchText = string.Empty;

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

    [ObservableProperty]
    private int registrationRequestCount;

    [ObservableProperty]
    private bool hasSelectedRegistrationRequest;

    [ObservableProperty]
    private bool hasSelectedUser;

    [ObservableProperty]
    private string selectedRequestTitle = "Заявка не выбрана";

    [ObservableProperty]
    private string selectedRequestDetails = "Выберите заявку слева, назначьте роль и активируйте пользователя.";

    [ObservableProperty]
    private string selectedUserDetails = "Выберите пользователя для редактирования или очистите форму для создания нового.";

    public UsersPageViewModel(UserRepository userRepository)
        : base("Пользователи")
    {
        this.userRepository = userRepository;
        Load();
    }

    public override void OnNavigatedTo()
    {
        Load();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedUserChanged(UserListItem? value)
    {
        if (value is null)
        {
            HasSelectedUser = false;
            return;
        }

        HasSelectedUser = true;
        FormTitle = "Редактирование пользователя";
        SelectedRoleId = value.RoleId;
        Username = value.Username;
        FullName = value.FullName;
        Email = value.Email ?? string.Empty;
        Phone = value.Phone ?? string.Empty;
        Password = string.Empty;
        IsActive = value.IsActive;
        SelectedUserDetails = $"{value.RoleName}. Логин: {value.Username}. " +
            $"Статус: {(value.IsActive ? "активен" : "отключен")}.";
    }

    partial void OnSelectedRegistrationRequestChanged(UserListItem? value)
    {
        if (value is null)
        {
            HasSelectedRegistrationRequest = false;
            SelectedRequestTitle = "Заявка не выбрана";
            SelectedRequestDetails = "Выберите заявку слева, назначьте роль и активируйте пользователя.";
            return;
        }

        HasSelectedRegistrationRequest = true;
        SelectedUser = value;
        SelectedRequestTitle = value.FullName;
        SelectedRequestDetails = $"Логин: {value.Username}. Email: {value.Email ?? "не указан"}. " +
            $"Телефон: {value.Phone ?? "не указан"}.";
        ResultMessage = $"Выбрана заявка: {value.FullName}. Назначьте роль и активируйте пользователя.";
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            Roles = new ObservableCollection<Role>(userRepository.GetRoles());
            allUsers = userRepository.GetUsers();
            RegistrationRequests = new ObservableCollection<UserListItem>(
                allUsers.Where(IsRegistrationRequest));
            ApplyFilter();
            UserCount = allUsers.Count;
            ActiveUserCount = allUsers.Count(user => user.IsActive);
            InactiveUserCount = allUsers.Count - ActiveUserCount;
            RegistrationRequestCount = RegistrationRequests.Count;
            SelectedRoleId = Roles.FirstOrDefault()?.RoleId ?? 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить пользователей: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void ActivateRegistrationRequest()
    {
        if (SelectedRegistrationRequest is null)
        {
            ResultMessage = "Сначала выберите заявку.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (SelectedRoleId == 0)
        {
            ResultMessage = "Выберите роль для пользователя.";
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            userRepository.UpdateUser(new User(
                SelectedRegistrationRequest.UserId,
                SelectedRoleId,
                SelectedRegistrationRequest.Username,
                string.Empty,
                SelectedRegistrationRequest.FullName,
                SelectedRegistrationRequest.Email,
                SelectedRegistrationRequest.Phone,
                true,
                SelectedRegistrationRequest.CreatedAt,
                null));

            ResultMessage = $"Пользователь {SelectedRegistrationRequest.FullName} активирован.";
            NotifySuccess(ResultMessage);
            SelectedRegistrationRequest = null;
            ClearForm();
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось активировать заявку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private async Task RejectRegistrationRequest()
    {
        if (SelectedRegistrationRequest is null)
        {
            ResultMessage = "Сначала выберите заявку.";
            NotifyWarning(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Отклонить заявку",
            $"Отклонить заявку пользователя {SelectedRegistrationRequest.FullName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            userRepository.DeleteUser(SelectedRegistrationRequest.UserId);
            ResultMessage = "Заявка отклонена.";
            NotifySuccess(ResultMessage);
            SelectedRegistrationRequest = null;
            ClearForm();
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось отклонить заявку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedRoleId == 0)
        {
            ResultMessage = "Выберите роль.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(FullName))
        {
            ResultMessage = "Заполните логин и ФИО.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (SelectedUser is null && string.IsNullOrWhiteSpace(Password))
        {
            ResultMessage = "Для нового пользователя нужен пароль.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!InputValidator.IsEmailValid(Email))
        {
            ResultMessage = "Email указан некорректно.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!InputValidator.IsPhoneValid(Phone))
        {
            ResultMessage = "Телефон должен содержать от 10 до 15 цифр.";
            NotifyWarning(ResultMessage);
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
                NotifySuccess(ResultMessage);
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
                NotifySuccess(ResultMessage);
            }

            ClearForm();
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить пользователя: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedUser = null;
        SelectedRegistrationRequest = null;
        HasSelectedUser = false;
        HasSelectedRegistrationRequest = false;
        FormTitle = "Новый пользователь";
        SelectedRoleId = Roles.FirstOrDefault()?.RoleId ?? 0;
        Username = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Password = string.Empty;
        IsActive = true;
        SelectedRequestTitle = "Заявка не выбрана";
        SelectedRequestDetails = "Выберите заявку слева, назначьте роль и активируйте пользователя.";
        SelectedUserDetails = "Заполните форму, чтобы создать пользователя вручную.";
    }

    [RelayCommand]
    private void ToggleActive()
    {
        if (SelectedUser is null)
        {
            ResultMessage = "Сначала выберите пользователя.";
            NotifyWarning(ResultMessage);
            return;
        }

        IsActive = !IsActive;
        Save();
    }

    [RelayCommand]
    private async Task DeleteSelectedUser()
    {
        if (SelectedUser is null)
        {
            ResultMessage = "Сначала выберите пользователя.";
            NotifyWarning(ResultMessage);
            return;
        }

        var references = userRepository.CountUserReferences(SelectedUser.UserId);
        if (references > 0)
        {
            ResultMessage = "Пользователь связан с журналом. Его можно отключить, но нельзя удалить без потери истории.";
            NotifyInfo(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить пользователя",
            $"Удалить пользователя {SelectedUser.FullName}? Это действие нельзя отменить.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            userRepository.DeleteUser(SelectedUser.UserId);
            ClearForm();
            Load();
            ResultMessage = "Пользователь удален.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить пользователя: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var visibleUsers = allUsers.Where(user => !IsRegistrationRequest(user)).ToList();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? visibleUsers
            : visibleUsers.Where(user =>
                user.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                user.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                user.RoleName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                user.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                user.Phone?.Contains(query, StringComparison.OrdinalIgnoreCase) == true).ToList();

        Users = new ObservableCollection<UserListItem>(filtered);
    }

    private static bool IsRegistrationRequest(UserListItem user) =>
        !user.IsActive && user.RoleName == "Преподаватель";
}
