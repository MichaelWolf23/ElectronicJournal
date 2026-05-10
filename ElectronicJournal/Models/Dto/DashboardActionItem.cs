using System.Windows.Input;

namespace ElectronicJournal.Models.Dto;

public sealed record DashboardActionItem(
    string Title,
    string Description,
    string TargetSection,
    ICommand OpenCommand);
