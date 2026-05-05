namespace ElectronicJournal.Models.Dto;

public sealed record CuratorNotificationItem(
    int NotificationId,
    string CuratorName,
    string? StudentName,
    string? GroupName,
    string Title,
    string Message,
    string Status,
    string CreatedAt,
    string? ReadAt);
