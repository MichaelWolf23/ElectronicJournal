using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class DebtorsPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly NotificationRepository notificationRepository;
    private readonly SettingsRepository settingsRepository;

    [ObservableProperty]
    private ObservableCollection<DebtorItem> debtors = new();

    [ObservableProperty]
    private DebtorItem? selectedDebtor;

    [ObservableProperty]
    private double minPositiveGrade = 3;

    [ObservableProperty]
    private string resultMessage = "Выберите должника, чтобы создать уведомление куратору.";

    public DebtorsPageViewModel(
        GradeRepository gradeRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository)
        : base("Должники")
    {
        this.gradeRepository = gradeRepository;
        this.notificationRepository = notificationRepository;
        this.settingsRepository = settingsRepository;

        Load();
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            MinPositiveGrade = settingsRepository.GetMinPositiveGrade();
            Debtors = new ObservableCollection<DebtorItem>(gradeRepository.GetDebtors(MinPositiveGrade));
            ResultMessage = Debtors.Count == 0
                ? "Должников не найдено."
                : $"Найдено записей с оценкой ниже {MinPositiveGrade}: {Debtors.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить должников: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CreateNotification()
    {
        if (SelectedDebtor is null)
        {
            ResultMessage = "Сначала выберите строку с должником.";
            return;
        }

        var curatorUserId = notificationRepository.GetCuratorUserIdForGroup(SelectedDebtor.GroupId);
        if (curatorUserId is null)
        {
            ResultMessage = $"Для группы {SelectedDebtor.GroupName} не найден куратор.";
            return;
        }

        try
        {
            var title = $"Задолженность: {SelectedDebtor.StudentName}";
            var message =
                $"{SelectedDebtor.StudentName}, группа {SelectedDebtor.GroupName}: " +
                $"оценка {SelectedDebtor.GradeValue} по предмету \"{SelectedDebtor.SubjectName}\" " +
                $"от {SelectedDebtor.GradeDate}. Преподаватель: {SelectedDebtor.TeacherName}.";

            notificationRepository.CreateNotification(new CuratorNotification(
                0,
                curatorUserId.Value,
                SelectedDebtor.StudentId,
                SelectedDebtor.GroupId,
                SelectedDebtor.AssignmentId,
                title,
                message,
                "Новое",
                string.Empty,
                null));

            ResultMessage = $"Уведомление куратору группы {SelectedDebtor.GroupName} создано.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось создать уведомление: {ex.Message}";
        }
    }
}
