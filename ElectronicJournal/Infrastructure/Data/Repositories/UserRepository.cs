using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class UserRepository : RepositoryBase
{
    public UserRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public AuthenticatedUser? FindActiveUserByUsername(string username)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                u.user_id,
                u.role_id,
                r.role_name,
                u.username,
                u.full_name,
                u.email
            FROM users u
            JOIN roles r ON r.role_id = u.role_id
            WHERE lower(u.username) = lower($username)
              AND u.is_active = 1;
            """;
        command.Parameters.AddWithValue("$username", username.Trim());

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new AuthenticatedUser(
                reader.GetInt32("user_id"),
                reader.GetInt32("role_id"),
                reader.GetString("role_name"),
                reader.GetString("username"),
                reader.GetString("full_name"),
                reader.GetNullableString("email"))
            : null;
    }

    public List<UserListItem> GetUsers()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                u.user_id,
                u.role_id,
                r.role_name,
                u.username,
                u.full_name,
                u.email,
                u.phone,
                u.is_active,
                u.created_at,
                u.updated_at
            FROM users u
            JOIN roles r ON r.role_id = u.role_id
            ORDER BY u.is_active DESC, r.role_name, u.full_name;
            """;

        using var reader = command.ExecuteReader();
        var users = new List<UserListItem>();

        while (reader.Read())
        {
            users.Add(new UserListItem(
                reader.GetInt32("user_id"),
                reader.GetInt32("role_id"),
                reader.GetString("role_name"),
                reader.GetString("username"),
                reader.GetString("full_name"),
                reader.GetNullableString("email"),
                reader.GetNullableString("phone"),
                reader.GetBooleanFromInt("is_active"),
                reader.GetString("created_at"),
                reader.GetNullableString("updated_at")));
        }

        return users;
    }

    public string? GetPasswordHash(string username)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT password_hash
            FROM users
            WHERE lower(username) = lower($username)
              AND is_active = 1;
            """;
        command.Parameters.AddWithValue("$username", username.Trim());

        return command.ExecuteScalar() as string;
    }

    public List<Role> GetRoles()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT role_id, role_name, description
            FROM roles
            ORDER BY role_name;
            """;

        using var reader = command.ExecuteReader();
        var roles = new List<Role>();

        while (reader.Read())
        {
            roles.Add(new Role(
                reader.GetInt32("role_id"),
                reader.GetString("role_name"),
                reader.GetNullableString("description")));
        }

        return roles;
    }

    public List<LookupItem> GetActiveUserLookupsByRole(string roleName)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.user_id, u.full_name
            FROM users u
            JOIN roles r ON r.role_id = u.role_id
            WHERE r.role_name = $role_name
              AND u.is_active = 1
            ORDER BY u.full_name;
            """;
        command.Parameters.AddWithValue("$role_name", roleName);

        using var reader = command.ExecuteReader();
        var users = new List<LookupItem>();

        while (reader.Read())
        {
            users.Add(new LookupItem(
                reader.GetInt32("user_id"),
                reader.GetString("full_name")));
        }

        return users;
    }

    public int CreateUser(User user)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (
                role_id,
                username,
                password_hash,
                full_name,
                email,
                phone,
                is_active)
            VALUES (
                $role_id,
                $username,
                $password_hash,
                $full_name,
                $email,
                $phone,
                1);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$role_id", user.RoleId);
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$password_hash", user.PasswordHash);
        command.Parameters.AddWithValue("$full_name", user.FullName);
        command.Parameters.AddWithValue("$email", (object?)user.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$phone", (object?)user.Phone ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void UpdateUser(User user)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE users
            SET role_id = $role_id,
                username = $username,
                full_name = $full_name,
                email = $email,
                phone = $phone,
                is_active = $is_active,
                updated_at = CURRENT_TIMESTAMP
            WHERE user_id = $user_id;
            """;
        command.Parameters.AddWithValue("$user_id", user.UserId);
        command.Parameters.AddWithValue("$role_id", user.RoleId);
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$full_name", user.FullName);
        command.Parameters.AddWithValue("$email", (object?)user.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$phone", (object?)user.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$is_active", user.IsActive ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void UpdatePassword(int userId, string passwordHash)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE users
            SET password_hash = $password_hash,
                updated_at = CURRENT_TIMESTAMP
            WHERE user_id = $user_id;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.Parameters.AddWithValue("$password_hash", passwordHash);
        command.ExecuteNonQuery();
    }

    public int CountUserReferences(int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM teacher_assignments WHERE teacher_user_id = $user_id) +
                (SELECT COUNT(*) FROM group_curators WHERE curator_user_id = $user_id) +
                (SELECT COUNT(*) FROM grades WHERE created_by_user_id = $user_id) +
                (SELECT COUNT(*) FROM grade_retakes WHERE changed_by_user_id = $user_id) +
                (SELECT COUNT(*) FROM final_grades WHERE approved_by_user_id = $user_id) +
                (SELECT COUNT(*) FROM curator_notifications WHERE curator_user_id = $user_id);
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void DeleteUser(int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM users
            WHERE user_id = $user_id;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.ExecuteNonQuery();
    }
}
