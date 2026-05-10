namespace ElectronicJournal.Models.Dto;

public sealed record AuthenticatedUser(
    int UserId,
    int RoleId,
    string RoleName,
    string Username,
    string FullName,
    string? Email);
