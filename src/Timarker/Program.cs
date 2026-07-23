using Timarker.Services;
using Timarker.Models;

namespace Timarker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
#if DEBUG
        ModelSelfCheck();
        if (Environment.GetEnvironmentVariable("TIMARKER_SELF_CHECK") == "1") return;
#endif
        using var singleInstance = new Mutex(true, "Timarker.SingleInstance.2026", out var createdNew);
        using var legacySingleInstance = new Mutex(true, "Timeline.SingleInstance.2026", out var legacyCreatedNew);
        if (!createdNew || !legacyCreatedNew)
        {
            var settings = new SettingsStore().Load();
            L.Use(settings);
            MessageBox.Show(
                L.IsEnglish ? "Timarker is already running. Find it in the system tray." : "事刻已经在运行了，可以在右下角系统托盘中找到它。",
                L.IsEnglish ? L.BrandEn : L.BrandZh,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        var store = new EventStore(AppPaths.DataDirectory);
        MainForm mainForm;
        do
        {
            mainForm = new MainForm(store);
            Application.Run(mainForm);
        } while (mainForm.RestartRequested);
    }

    private static void ModelSelfCheck()
    {
        var item = new EventItem
        {
            Type = EventType.Anniversary,
            StartAt = new DateTime(2020, 1, 1, 9, 0, 0),
            AnniversaryMode = AnniversaryMode.Days,
            MilestoneDays = "10, 100"
        };
        System.Diagnostics.Debug.Assert(item.NextDueAt(new DateTime(2020, 1, 5)) == new DateTime(2020, 1, 11, 9, 0, 0));
        item.Complete(new DateTime(2020, 1, 11, 10, 0, 0));
        System.Diagnostics.Debug.Assert(item.Status is EventStatus.Pending);
        System.Diagnostics.Debug.Assert(item.Occurrences.Single().Status is EventStatus.Done);
        System.Diagnostics.Debug.Assert(item.NextDueAt(new DateTime(2020, 1, 11, 10, 0, 0)) == new DateTime(2020, 4, 10, 9, 0, 0));

        var birthday = new EventItem
        {
            Type = EventType.Birthday,
            StartAt = new DateTime(2000, 7, 21, 9, 0, 0),
            BirthdayMonth = 7,
            BirthdayDay = 21
        };
        birthday.Complete(new DateTime(2026, 7, 21, 12, 0, 0));
        System.Diagnostics.Debug.Assert(birthday.Status is EventStatus.Pending);
        System.Diagnostics.Debug.Assert(birthday.NextDueAt(new DateTime(2026, 7, 21, 12, 0, 0)) == new DateTime(2027, 7, 21, 9, 0, 0));

        var recurring = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 7, 14, 9, 0, 0),
            RepeatUnit = RepeatUnit.Day
        };
        recurring.Complete(new DateTime(2026, 7, 14, 10, 0, 0));
        System.Diagnostics.Debug.Assert(recurring.Status is EventStatus.Pending);
        System.Diagnostics.Debug.Assert(recurring.Occurrences.Single().Status is EventStatus.Done);
        System.Diagnostics.Debug.Assert(recurring.NextDueAt(new DateTime(2026, 7, 14, 10, 0, 0)) == new DateTime(2026, 7, 15, 9, 0, 0));

        var weekdays = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 7, 17, 9, 0, 0),
            RepeatUnit = RepeatUnit.Day,
            RepeatPattern = RepeatPattern.Weekdays
        };
        weekdays.Complete(new DateTime(2026, 7, 17, 10, 0, 0));
        System.Diagnostics.Debug.Assert(weekdays.NextDueAt(new DateTime(2026, 7, 17, 10, 0, 0)) == new DateTime(2026, 7, 20, 9, 0, 0));

        var selectedDays = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 7, 14, 9, 0, 0),
            RepeatUnit = RepeatUnit.Week,
            RepeatPattern = RepeatPattern.SelectedWeekdays,
            RepeatDaysOfWeek = [DayOfWeek.Wednesday, DayOfWeek.Friday]
        };
        System.Diagnostics.Debug.Assert(selectedDays.NextDueAt(new DateTime(2026, 7, 14, 9, 0, 0)) == new DateTime(2026, 7, 15, 9, 0, 0));
        System.Diagnostics.Debug.Assert(selectedDays.OccursOn(new DateTime(2026, 7, 17)));
        System.Diagnostics.Debug.Assert(!selectedDays.OccursOn(new DateTime(2026, 7, 16)));
        selectedDays.SetRecurrencePaused(true);
        System.Diagnostics.Debug.Assert(selectedDays.NextDueAt(new DateTime(2026, 7, 14, 9, 0, 0)) is null);
        selectedDays.SetRecurrencePaused(false);

        var missedWeekly = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 7, 13, 9, 0, 0),
            RepeatUnit = RepeatUnit.Week,
            MissedOccurrencePolicy = MissedOccurrencePolicy.SkipToNext
        };
        System.Diagnostics.Debug.Assert(missedWeekly.NextDueAt(new DateTime(2026, 7, 14, 12, 0, 0)) == new DateTime(2026, 7, 20, 9, 0, 0));

        var monthly = new EventItem
        {
            Type = EventType.Recurring,
            StartAt = new DateTime(2026, 3, 1, 9, 0, 0),
            RepeatUnit = RepeatUnit.Month,
            RepeatPattern = RepeatPattern.MonthlyNthWeekday,
            RepeatWeekOfMonth = 2,
            RepeatDayOfWeek = DayOfWeek.Monday,
            RecurrenceEndMode = RecurrenceEndMode.AfterCount,
            RepeatCount = 2
        };
        System.Diagnostics.Debug.Assert(monthly.NextDueAt(new DateTime(2026, 3, 1)) == new DateTime(2026, 3, 9, 9, 0, 0));
        monthly.Complete(new DateTime(2026, 3, 9, 10, 0, 0));
        System.Diagnostics.Debug.Assert(monthly.NextDueAt(new DateTime(2026, 3, 9, 10, 0, 0)) == new DateTime(2026, 4, 13, 9, 0, 0));
        monthly.Complete(new DateTime(2026, 4, 13, 10, 0, 0));
        System.Diagnostics.Debug.Assert(monthly.NextDueAt(new DateTime(2026, 4, 13, 10, 0, 0)) is null);

        var project = new EventItem
        {
            IsProject = true,
            Title = "测试项目",
            DeadlineAt = new DateTime(2026, 8, 1, 18, 0, 0)
        };
        var projectStep = new EventItem
        {
            ProjectId = project.Id,
            ProjectOrder = 1,
            StartAt = new DateTime(2026, 7, 20, 9, 0, 0)
        };
        System.Diagnostics.Debug.Assert(project.NextDueAt(new DateTime(2026, 7, 20)) is null);
        projectStep.ShiftSchedule(1440);
        System.Diagnostics.Debug.Assert(projectStep.StartAt == new DateTime(2026, 7, 21, 9, 0, 0));
        projectStep.Complete();
        System.Diagnostics.Debug.Assert(projectStep.Status is EventStatus.Done);

        var reminder = new EventItem
        {
            StartAt = new DateTime(2026, 7, 14, 9, 0, 0),
            ReminderRepeatMinutes = 10,
            ReminderRepeatCount = 1
        };
        var firstReminderAt = new DateTime(2026, 7, 14, 9, 0, 0);
        System.Diagnostics.Debug.Assert(reminder.IsDue(firstReminderAt));
        reminder.MarkReminded(firstReminderAt);
        System.Diagnostics.Debug.Assert(!reminder.IsDue(firstReminderAt.AddMinutes(5)));
        System.Diagnostics.Debug.Assert(reminder.IsDue(firstReminderAt.AddMinutes(10)));
        reminder.MarkReminded(firstReminderAt.AddMinutes(10));
        System.Diagnostics.Debug.Assert(!reminder.IsDue(firstReminderAt.AddMinutes(20)));
    }
}
