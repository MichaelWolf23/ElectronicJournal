using System;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.Services;

public sealed class AuthService
{
    private readonly UserRepository userRepository;

    public AuthService(UserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public AuthenticatedUser? Login(string username, string password)
    {
        var storedHash = userRepository.GetPasswordHash(username);
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return null;
        }

        var passwordHash = PasswordHasher.Hash(password);
        var isModernHash = storedHash == passwordHash;
        var normalizedUsername = username.Trim();
        var isDemoLegacyHash = storedHash == $"hash_{normalizedUsername}" && password == normalizedUsername;

        return isModernHash || isDemoLegacyHash
            ? userRepository.FindActiveUserByUsername(username)
            : null;
    }

    public void RegisterInactiveTeacher(
        string username,
        string password,
        string fullName,
        string? email,
        string? phone)
    {
        var teacherRoleId = userRepository.GetRoleIdByName("Преподаватель");
        if (teacherRoleId is null)
        {
            throw new InvalidOperationException("В базе данных не найдена роль \"Преподаватель\".");
        }

        userRepository.CreateUser(new User(
            0,
            teacherRoleId.Value,
            username.Trim(),
            PasswordHasher.Hash(password),
            fullName.Trim(),
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            false,
            string.Empty,
            null));
    }
}
