using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class NotificationRepository : RepositoryBase
{
    public NotificationRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<CuratorNotificationItem> GetNotifications()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                n.notification_id,
                curator.full_name AS curator_name,
                s.full_name AS student_name,
                g.group_name,
                n.title,
                n.message,
                n.status,
                n.created_at,
                n.read_at
            FROM curator_notifications n
            JOIN users curator ON curator.user_id = n.curator_user_id
            LEFT JOIN students s ON s.student_id = n.student_id
            LEFT JOIN groups g ON g.group_id = n.group_id
            ORDER BY n.created_at DESC;
            """;

        using var reader = command.ExecuteReader();
        var notifications = new List<CuratorNotificationItem>();

        while (reader.Read())
        {
            notifications.Add(new CuratorNotificationItem(
                reader.GetInt32("notification_id"),
                reader.GetString("curator_name"),
                reader.GetNullableString("student_name"),
                reader.GetNullableString("group_name"),
                reader.GetString("title"),
                reader.GetString("message"),
                reader.GetString("status"),
                reader.GetString("created_at"),
                reader.GetNullableString("read_at")));
        }

        return notifications;
    }

    public List<CuratorNotificationItem> GetNotificationsByCurator(int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                n.notification_id,
                curator.full_name AS curator_name,
                s.full_name AS student_name,
                g.group_name,
                n.title,
                n.message,
                n.status,
                n.created_at,
                n.read_at
            FROM curator_notifications n
            JOIN users curator ON curator.user_id = n.curator_user_id
            LEFT JOIN students s ON s.student_id = n.student_id
            LEFT JOIN groups g ON g.group_id = n.group_id
            WHERE n.curator_user_id = $curator_user_id
            ORDER BY n.created_at DESC;
            """;
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        using var reader = command.ExecuteReader();
        var notifications = new List<CuratorNotificationItem>();

        while (reader.Read())
        {
            notifications.Add(new CuratorNotificationItem(
                reader.GetInt32("notification_id"),
                reader.GetString("curator_name"),
                reader.GetNullableString("student_name"),
                reader.GetNullableString("group_name"),
                reader.GetString("title"),
                reader.GetString("message"),
                reader.GetString("status"),
                reader.GetString("created_at"),
                reader.GetNullableString("read_at")));
        }

        return notifications;
    }

    public int CreateNotification(CuratorNotification notification)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO curator_notifications (
                curator_user_id,
                student_id,
                group_id,
                assignment_id,
                title,
                message,
                status)
            VALUES (
                $curator_user_id,
                $student_id,
                $group_id,
                $assignment_id,
                $title,
                $message,
                $status);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$curator_user_id", notification.CuratorUserId);
        command.Parameters.AddWithValue("$student_id", (object?)notification.StudentId ?? DBNull.Value);
        command.Parameters.AddWithValue("$group_id", (object?)notification.GroupId ?? DBNull.Value);
        command.Parameters.AddWithValue("$assignment_id", (object?)notification.AssignmentId ?? DBNull.Value);
        command.Parameters.AddWithValue("$title", notification.Title);
        command.Parameters.AddWithValue("$message", notification.Message);
        command.Parameters.AddWithValue("$status", notification.Status);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public CuratorLookup? GetCuratorForGroup(int groupId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.user_id, u.full_name
            FROM group_curators gc
            JOIN users u ON u.user_id = gc.curator_user_id
            WHERE gc.group_id = $group_id
              AND u.is_active = 1
            ORDER BY gc.assigned_at DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$group_id", groupId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new CuratorLookup(reader.GetInt32("user_id"), reader.GetString("full_name"), true);
        }

        return GetFallbackCurator(connection);
    }

    private static CuratorLookup? GetFallbackCurator(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.user_id, u.full_name
            FROM users u
            JOIN roles r ON r.role_id = u.role_id
            WHERE r.role_name = 'Куратор группы'
              AND u.is_active = 1
            ORDER BY u.full_name
            LIMIT 1;
            """;

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new CuratorLookup(reader.GetInt32("user_id"), reader.GetString("full_name"), false)
            : null;
    }

    public void UpdateStatus(int notificationId, string status)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE curator_notifications
            SET status = $status,
                read_at = CASE
                    WHEN $status = 'Прочитано' THEN CURRENT_TIMESTAMP
                    ELSE read_at
                END
            WHERE notification_id = $notification_id;
            """;
        command.Parameters.AddWithValue("$notification_id", notificationId);
        command.Parameters.AddWithValue("$status", status);
        command.ExecuteNonQuery();
    }

    public void DeleteNotification(int notificationId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM curator_notifications
            WHERE notification_id = $notification_id;
            """;
        command.Parameters.AddWithValue("$notification_id", notificationId);
        command.ExecuteNonQuery();
    }
}

public sealed record CuratorLookup(int UserId, string FullName, bool IsAssignedToGroup);

