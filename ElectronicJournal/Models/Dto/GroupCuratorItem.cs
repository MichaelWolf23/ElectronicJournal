namespace ElectronicJournal.Models.Dto;

public sealed record GroupCuratorItem(
    int GroupCuratorId,
    string GroupName,
    string CuratorName,
    string AssignedAt);
