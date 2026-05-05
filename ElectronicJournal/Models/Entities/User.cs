namespace ElectronicJournal.Models.Entities;

public sealed record User(
    int UserId,
    int RoleId,
    string Username,
    string PasswordHash,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
    string CreatedAt,
    string? UpdatedAt);
