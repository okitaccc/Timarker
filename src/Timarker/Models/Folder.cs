namespace Timarker.Models;

public sealed class Folder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<Guid> EventIds { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool Contains(Guid eventId) => EventIds.Contains(eventId);
    public void Add(Guid eventId) { if (!Contains(eventId)) EventIds.Add(eventId); }
    public void Remove(Guid eventId) => EventIds.Remove(eventId);
    public void NormalizeAfterLoad() { Name ??= ""; EventIds ??= []; }
}
