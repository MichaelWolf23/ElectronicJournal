namespace ElectronicJournal.ViewModels;

public sealed class PlaceholderPageViewModel : PageViewModelBase
{
    public PlaceholderPageViewModel(string title, string description)
        : base(title)
    {
        Description = description;
    }

    public string Description { get; }
}
