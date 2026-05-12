using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class SettingsRepository : RepositoryBase
{
    public SettingsRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<SystemSetting> GetSettings()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT setting_key, setting_value, description, updated_at
            FROM system_settings
            ORDER BY setting_key;
            """;

        using var reader = command.ExecuteReader();
        var settings = new List<SystemSetting>();

        while (reader.Read())
        {
            settings.Add(new SystemSetting(
                reader.GetString("setting_key"),
                reader.GetString("setting_value"),
                reader.GetNullableString("description"),
                reader.GetString("updated_at")));
        }

        return settings;
    }

    public string? GetValue(string settingKey)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT setting_value
            FROM system_settings
            WHERE setting_key = $setting_key;
            """;
        command.Parameters.AddWithValue("$setting_key", settingKey);

        return command.ExecuteScalar() as string;
    }

    public double GetMinPositiveGrade()
    {
        var value = GetValue("Минимальная положительная оценка");
        return double.TryParse(value, out var minPositiveGrade)
            ? minPositiveGrade
            : 3;
    }

    public double GetMinGradeScale()
    {
        var value = GetValue("Минимальная оценка шкалы");
        return double.TryParse(value, out var minGrade)
            ? minGrade
            : 2;
    }

    public double GetMaxGradeScale()
    {
        var value = GetValue("Максимальная оценка шкалы");
        return double.TryParse(value, out var maxGrade)
            ? maxGrade
            : 5;
    }

    public bool AreCuratorNotificationsEnabled()
    {
        var value = GetValue("Автоматические уведомления кураторам");
        return value is null ||
            value.Equals("Включены", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("Да", StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateSetting(string settingKey, string settingValue)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE system_settings
            SET setting_value = $setting_value,
                updated_at = CURRENT_TIMESTAMP
            WHERE setting_key = $setting_key;
            """;
        command.Parameters.AddWithValue("$setting_key", settingKey);
        command.Parameters.AddWithValue("$setting_value", settingValue);
        command.ExecuteNonQuery();
    }
}

