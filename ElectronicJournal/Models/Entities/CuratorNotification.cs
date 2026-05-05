namespace ElectronicJournal.Models.Entities;

public sealed record CuratorNotification(
    int NotificationId,
    int CuratorUserId,
    int? StudentId,
    int? GroupId,
    int? AssignmentId,
    string Title,
    string Message,
    string Status,
    string CreatedAt,
    string? ReadAt);
