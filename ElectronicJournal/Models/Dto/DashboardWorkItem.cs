using System.Windows.Input;

namespace ElectronicJournal.Models.Dto;

public sealed record DashboardWorkItem(
    string Title,
    string Description,
    string Badge,
    string Accent,
    string TargetSection,
    ICommand OpenCommand);
