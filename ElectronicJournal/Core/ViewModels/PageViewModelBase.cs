using CommunityToolkit.Mvvm.ComponentModel;
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
