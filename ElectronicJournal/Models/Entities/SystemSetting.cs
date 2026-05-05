namespace ElectronicJournal.Models.Entities;

public sealed record SystemSetting(
    string SettingKey,
    string SettingValue,
    string? Description,
    string UpdatedAt);
