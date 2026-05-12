using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ElectronicJournal.Services;

public static class CsvExportService
{
    public static string CreateReport(string reportName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string?>> rows)
    {
        var reportsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "ElectronicJournal");
        Directory.CreateDirectory(reportsDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var fileName = $"{NormalizeFileName(reportName)}_{timestamp}.csv";
        var reportPath = Path.Combine(reportsDirectory, fileName);

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(';', headers.Select(Escape)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(';', row.Select(Escape)));
        }

        File.WriteAllText(reportPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return reportPath;
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        value = value.Replace("\"", "\"\"");
        return value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value}\""
            : value;
    }

    private static string NormalizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "report" : safe;
    }
}
