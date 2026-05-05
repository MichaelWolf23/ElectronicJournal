using CommunityToolkit.Mvvm.ComponentModel;

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
}
