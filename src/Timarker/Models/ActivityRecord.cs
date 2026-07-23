namespace Timarker.Models;

public enum ActivityRecordKind
{
    Note,
    EventCompleted,
    ProjectCompleted,
    BirthdayCelebrated,
    AnniversaryCelebrated
}

public sealed class ActivityRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ActivityRecordKind Kind { get; set; }
    public Guid? EventId { get; set; }
    public Guid? ProjectId { get; set; }
    public List<Guid> PersonIds { get; set; } = [];
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    public void NormalizeAfterLoad()
    {
        PersonIds ??= [];
        Title ??= "";
        Detail ??= "";
    }
}
