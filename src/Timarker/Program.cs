using Timarker.Services;
using Timarker.Models;
using System.Text.Json;

namespace Timarker;

internal static class Program
{
    private const string ActivationEventName = "Timarker.Activate.2026";

    [STAThread]
    private static void Main()
    {
        CrashReporter.Initialize();
#if DEBUG
        ModelSelfCheck();
        if (Environment.GetEnvironmentVariable("TIMARKER_SELF_CHECK") == "1") return;
#endif
        using var singleInstance = new Mutex(true, "Timarker.SingleInstance.2026", out var createdNew);
        using var legacySingleInstance = new Mutex(true, "Timeline.SingleInstance.2026", out var legacyCreatedNew);
        if (!createdNew || !legacyCreatedNew)
        {
            if (EventWaitHandle.TryOpenExisting(ActivationEventName, out var existingActivationEvent))
            {
                using (existingActivationEvent) existingActivationEvent.Set();
            }
            return;
        }

        ApplicationConfiguration.Initialize();
        AppTheme.Use(new SettingsStore().Load());
        var store = new EventStore(AppPaths.DataDirectory);
        using var activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        MainForm mainForm;
        do
        {
            AppTheme.Use(new SettingsStore().Load());
            mainForm = new MainForm(store);
            var activationRegistration = ThreadPool.RegisterWaitForSingleObject(
                activationEvent,
                (_, _) =>
                {
                    if (!mainForm.IsDisposed && mainForm.IsHandleCreated)
                        mainForm.BeginInvoke(mainForm.RestoreFromTray);
                },
                null,
                Timeout.Infinite,
                false);
            Application.Run(mainForm);
            activationRegistration.Unregister(null);
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

        var projectStep = new EventItem { StartAt = new DateTime(2026, 7, 20, 9, 0, 0) };
        var project = new Project { Name = "测试项目", Steps = [new ProjectStep { EventId = projectStep.Id, Order = 1 }] };
        System.Diagnostics.Debug.Assert(project.Steps.Single().EventId == projectStep.Id);
        projectStep.ShiftSchedule(1440);
        System.Diagnostics.Debug.Assert(projectStep.StartAt == new DateTime(2026, 7, 21, 9, 0, 0));
        projectStep.Complete();
        System.Diagnostics.Debug.Assert(projectStep.Status is EventStatus.Done);

        var folder = new Folder { Name = "测试收藏夹" };
        folder.Add(projectStep.Id);
        folder.Add(projectStep.Id);
        System.Diagnostics.Debug.Assert(folder.EventIds.Count == 1 && folder.Contains(projectStep.Id));

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

        var migrationDirectory = Path.Combine(Path.GetTempPath(), $"timarker-migration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(migrationDirectory);
        try
        {
            var legacyFolder = new EventItem { Title = "旧收藏夹", IsGroup = true };
            var legacyProject = new EventItem { Title = "旧项目", IsProject = true };
            var legacyEvent = new EventItem { Title = "旧步骤", ProjectId = legacyProject.Id, ProjectOrder = 1, FolderIds = [legacyFolder.Id] };
            File.WriteAllText(Path.Combine(migrationDirectory, "events.json"), JsonSerializer.Serialize(new[] { legacyFolder, legacyProject, legacyEvent }));
            var migratedStore = new EventStore(migrationDirectory);
            var migratedEvents = migratedStore.Load();
            System.Diagnostics.Debug.Assert(migratedEvents.Count == 1 && migratedEvents[0].Id == legacyEvent.Id);
            System.Diagnostics.Debug.Assert(migratedStore.Folders.Single().Contains(legacyEvent.Id));
            System.Diagnostics.Debug.Assert(migratedStore.Projects.Single().Steps.Single().EventId == legacyEvent.Id);
        }
        finally
        {
            Directory.Delete(migrationDirectory, true);
        }

        var activityDirectory = Path.Combine(Path.GetTempPath(), $"timarker-activity-recovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(activityDirectory);
        try
        {
            var activityStore = new ActivityStore(activityDirectory);
            activityStore.Sessions.Add(new AppActivitySession { StartedAt = DateTime.Now.AddMinutes(-5), EndedAt = DateTime.Now, ProcessName = "test", AppName = "Test" });
            activityStore.Save(30);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.WriteAllText(Path.Combine(activityDirectory, "activity.db"), "damaged");
            var recoveredActivityStore = new ActivityStore(activityDirectory);
            System.Diagnostics.Debug.Assert(recoveredActivityStore.RecoveryMessage is not null);
            System.Diagnostics.Debug.Assert(recoveredActivityStore.Sessions.Count == 1);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(activityDirectory, true);
        }
    }
}
