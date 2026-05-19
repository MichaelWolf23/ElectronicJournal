using CommunityToolkit.Mvvm.ComponentModel;
using ElectronicJournal.Services;
using System.Windows.Input;

namespace ElectronicJournal.ViewModels;

public abstract partial class PageViewModelBase : ViewModelBase
{
    protected PageViewModelBase(string title)
    {
        Title = title;
    }

    public string Title { get; }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    partial void OnErrorMessageChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            NotifyError(value);
        }
    }

    protected void NotifySuccess(string message, string title = "Готово") =>
        NotificationService.Instance.Success(message, title);

    protected void NotifyWarning(string message, string title = "Проверьте") =>
        NotificationService.Instance.Warning(message, title);

    protected void NotifyError(string message, string title = "Ошибка") =>
        NotificationService.Instance.Error(message, title);

    protected void NotifyInfo(string message, string title = "Информация") =>
        NotificationService.Instance.Info(message, title);

    public virtual void OnNavigatedTo()
    {
        var loadCommandProperty = GetType().GetProperty("LoadCommand");
        if (loadCommandProperty?.GetValue(this) is ICommand loadCommand &&
            loadCommand.CanExecute(null))
        {
            loadCommand.Execute(null);
        }
    }
}
