using System;
using System.Globalization;
using System.IO;

namespace ElectronicJournal.Services;

public sealed record ArchivePeriodResult(string BackupPath, string PeriodName, int ArchivedRows);

public sealed class BackupService
{
    private readonly DatabaseService databaseService;

    public BackupService(DatabaseService databaseService)
    {
        this.databaseService = databaseService;
    }

    public string CreateBackup()
    {
        if (!File.Exists(databaseService.DatabasePath))
        {
            throw new FileNotFoundException("Файл базы данных не найден.", databaseService.DatabasePath);
        }

        var backupDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(backupDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var backupPath = Path.Combine(backupDirectory, $"electronic_journal_{timestamp}.db");
        File.Copy(databaseService.DatabasePath, backupPath, overwrite: false);

        return backupPath;
    }

    public ArchivePeriodResult ArchiveCurrentPeriod()
    {
        var backupPath = CreateBackup();

        using var connection = databaseService.CreateConnection();
        using var periodCommand = connection.CreateCommand();
        periodCommand.CommandText = """
            SELECT setting_value
            FROM system_settings
            WHERE setting_key = 'Текущий учебный период';
            """;

        var periodName = Convert.ToString(periodCommand.ExecuteScalar())?.Trim();
        if (string.IsNullOrWhiteSpace(periodName))
        {
            throw new InvalidOperationException("Не задан текущий учебный период.");
        }

        using var archiveCommand = connection.CreateCommand();
        archiveCommand.CommandText = """
            UPDATE academic_periods
            SET is_archived = 1
            WHERE period_name = $period_name
              AND is_archived = 0;
            """;
        archiveCommand.Parameters.AddWithValue("$period_name", periodName);
        var archivedRows = archiveCommand.ExecuteNonQuery();

        return new ArchivePeriodResult(backupPath, periodName, archivedRows);
    }
}
