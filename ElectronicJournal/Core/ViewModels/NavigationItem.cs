namespace ElectronicJournal.ViewModels;

public sealed record NavigationItem(
    string Title,
    PageViewModelBase Page,
    string Icon = "",
    string Description = "");
