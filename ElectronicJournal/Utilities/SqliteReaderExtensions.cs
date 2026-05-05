using Microsoft.Data.Sqlite;

namespace ElectronicJournal.Utilities;

public static class SqliteReaderExtensions
{
    public static int GetInt32(this SqliteDataReader reader, string name) =>
        reader.GetInt32(reader.GetOrdinal(name));

    public static int? GetNullableInt32(this SqliteDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetInt32(index);
    }

    public static double GetDouble(this SqliteDataReader reader, string name) =>
        reader.GetDouble(reader.GetOrdinal(name));

    public static double? GetNullableDouble(this SqliteDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetDouble(index);
    }

    public static string GetString(this SqliteDataReader reader, string name) =>
        reader.GetString(reader.GetOrdinal(name));

    public static string? GetNullableString(this SqliteDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : reader.GetString(index);
    }

    public static bool GetBooleanFromInt(this SqliteDataReader reader, string name) =>
        reader.GetInt32(reader.GetOrdinal(name)) == 1;
}
