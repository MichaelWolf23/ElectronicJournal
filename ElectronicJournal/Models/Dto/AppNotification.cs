using System;

namespace ElectronicJournal.Models.Dto;

public enum AppNotificationKind
{
    Success,
    Warning,
    Error,
    Info
}

public sealed record AppNotification(
    Guid Id,
    AppNotificationKind Kind,
    string Title,
    string Message);
