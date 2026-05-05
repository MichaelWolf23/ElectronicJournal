namespace ElectronicJournal.Models.Entities;

public sealed record Role(
    int RoleId,
    string RoleName,
    string? Description);
