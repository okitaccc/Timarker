using Timarker.Models;

namespace Timarker.Services;

public sealed class ReminderEngine
{
    private readonly NotifyIcon _notifyIcon;

    public ReminderEngine(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon;
    }

    public IReadOnlyList<EventItem> FindDueEvents(IEnumerable<EventItem> events, DateTime now)
    {
        return events.Where(e => e.IsDue(now)).ToList();
    }

    public void Notify(EventItem item)
    {
        _notifyIcon.BalloonTipTitle = "Timarker 提醒";
        _notifyIcon.BalloonTipText = item.Title;
        _notifyIcon.ShowBalloonTip(5000);
        if (item.Type is EventType.StartAt && item.Status is EventStatus.Pending)
        {
            item.Status = EventStatus.InProgress;
        }

        item.MarkReminded(DateTime.Now);
    }
}
