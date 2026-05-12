namespace ElectronicJournal.Models.Entities;

public sealed record SystemSetting(
    string SettingKey,
    string SettingValue,
    string? Description,
    string UpdatedAt)
{
    public string AppliesTo => SettingKey switch
    {
        "Минимальная положительная оценка" => "Отчеты, должники, студенты риска",
        "Минимальная оценка шкалы" => "Проверка оценок при вводе",
        "Максимальная оценка шкалы" => "Проверка оценок при вводе",
        "Текущий учебный период" => "Шапка приложения, архив семестра",
        "Автоматические уведомления кураторам" => "Разрешает или отключает создание уведомлений",
        _ => "Системный параметр"
    };
}
