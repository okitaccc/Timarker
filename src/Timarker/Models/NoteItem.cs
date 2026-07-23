namespace Timarker.Models;

public enum NoteKind
{
    General,
    Work,
    Study
}

public sealed class NoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Tags { get; set; } = "";
    public NoteKind Kind { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public void NormalizeAfterLoad()
    {
        Title ??= "";
        Content ??= "";
        Tags ??= "";
    }
}
