using System;
using Microsoft.Data.Sqlite;

namespace ElectronicJournal.Utilities;

public static class UserMessageHelper
{
    public static string ToFriendlyDatabaseError(Exception exception)
    {
        if (exception is SqliteException sqliteException)
        {
            return sqliteException.SqliteErrorCode switch
            {
                19 => "Данные не сохранены: нарушено ограничение базы. Проверьте, что запись не дублируется и все выбранные значения существуют.",
                _ => "База данных не смогла выполнить операцию. Проверьте введенные данные и попробуйте еще раз."
            };
        }

        return exception.Message;
    }
}
