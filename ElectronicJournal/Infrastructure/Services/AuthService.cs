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

    public AuthenticatedUser Register(
        int roleId,
        string username,
        string password,
        string fullName,
        string? email,
        string? phone)
    {
        userRepository.CreateUser(new User(
            0,
            roleId,
            username.Trim(),
            PasswordHasher.Hash(password),
            fullName.Trim(),
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            true,
            string.Empty,
            null));

        return Login(username, password)!;
    }
}
