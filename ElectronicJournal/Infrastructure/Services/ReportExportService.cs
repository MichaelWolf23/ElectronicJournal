using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ElectronicJournal.Services;

public static class ReportExportService
{
    public static string CreatePrintableHtml(
        string reportName,
        string title,
        IReadOnlyList<string> meta,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string?>> rows)
    {
        var path = CreateReportPath(reportName, "html");
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
        builder.AppendLine($"<title>{Html(title)}</title>");
        builder.AppendLine("""
            <style>
                body{font-family:Arial,sans-serif;margin:28px;color:#111827}
                h1{font-size:22px;margin:0 0 8px}
                .meta{color:#4b5563;margin:0 0 18px;line-height:1.45}
                table{width:100%;border-collapse:collapse;font-size:13px}
                th,td{border:1px solid #d1d5db;padding:7px 8px;text-align:left;vertical-align:top}
                th{background:#f3f4f6}
                @media print{button{display:none}body{margin:12mm}}
            </style>
            <script>
                window.addEventListener('load', function(){ setTimeout(function(){ window.print(); }, 300); });
            </script>
            </head><body>
            <button onclick="window.print()">Печать</button>
            """);
        builder.AppendLine($"<h1>{Html(title)}</h1>");
        builder.AppendLine("<div class=\"meta\">");
        foreach (var item in meta)
        {
            builder.AppendLine($"{Html(item)}<br>");
        }
        builder.AppendLine("</div><table><thead><tr>");
        foreach (var header in headers)
        {
            builder.AppendLine($"<th>{Html(header)}</th>");
        }

        builder.AppendLine("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            foreach (var value in row)
            {
                builder.AppendLine($"<td>{Html(value ?? string.Empty)}</td>");
            }
            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</tbody></table></body></html>");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }

    public static string CreateExcelXml(
        string reportName,
        string title,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string?>> rows)
    {
        var path = CreateReportPath(reportName, "xls");
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        builder.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        builder.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        builder.AppendLine($"<Worksheet ss:Name=\"{Xml(TrimSheetName(title))}\"><Table>");
        builder.AppendLine("<Row>");
        foreach (var header in headers)
        {
            builder.AppendLine($"<Cell><Data ss:Type=\"String\">{Xml(header)}</Data></Cell>");
        }
        builder.AppendLine("</Row>");

        foreach (var row in rows)
        {
            builder.AppendLine("<Row>");
            foreach (var value in row)
            {
                builder.AppendLine($"<Cell><Data ss:Type=\"String\">{Xml(value ?? string.Empty)}</Data></Cell>");
            }
            builder.AppendLine("</Row>");
        }

        builder.AppendLine("</Table></Worksheet></Workbook>");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }

    public static void OpenPrintDialog(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public static void ShowInExplorer(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{path}\"",
            UseShellExecute = true
        });
    }

    private static string CreateReportPath(string reportName, string extension)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "ElectronicJournal");
        Directory.CreateDirectory(directory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(directory, $"{NormalizeFileName(reportName)}_{timestamp}.{extension}");
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string Xml(string value) => WebUtility.HtmlEncode(value);

    private static string TrimSheetName(string value) => value.Length > 28 ? value[..28] : value;

    private static string NormalizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "report" : safe;
    }
}
