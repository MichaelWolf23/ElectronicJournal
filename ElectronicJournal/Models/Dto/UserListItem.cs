namespace ElectronicJournal.Models.Dto;

public sealed record UserListItem(
    int UserId,
    int RoleId,
    string RoleName,
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
    string CreatedAt,
    string? UpdatedAt);
