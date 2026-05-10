using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ElectronicJournal.Services;

public sealed class DatabaseService
{
    private const string DatabaseFileName = "electronic_journal.db";

    public string DatabasePath { get; }

    public DatabaseService()
    {
        DatabasePath = Path.Combine(AppContext.BaseDirectory, DatabaseFileName);
    }

    public SqliteConnection CreateConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWrite,
            ForeignKeys = true
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

    public DatabaseHealth CheckConnection()
    {
        if (!File.Exists(DatabasePath))
        {
            return new DatabaseHealth(
                false,
                DatabasePath,
                0,
                "Файл базы данных не найден.");
        }

        try
        {
            using var connection = CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table';";

            var tableCount = Convert.ToInt32(command.ExecuteScalar());
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name IN ('roles', 'users', 'students', 'grades', 'system_settings');
                """;
            var requiredTableCount = Convert.ToInt32(command.ExecuteScalar());
            if (requiredTableCount < 5)
            {
                return new DatabaseHealth(
                    false,
                    DatabasePath,
                    tableCount,
                    "Файл базы данных найден, но в нем нет обязательных таблиц электронного журнала.");
            }

            return new DatabaseHealth(
                true,
                DatabasePath,
                tableCount,
                null);
        }
        catch (Exception ex)
        {
            return new DatabaseHealth(
                false,
                DatabasePath,
                0,
                ex.Message);
        }
    }
}
