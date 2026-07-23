namespace Timarker.Models;

public sealed class PersonProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Tags { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public void NormalizeAfterLoad()
    {
        Name ??= "";
        Relationship ??= "";
        Notes ??= "";
        Tags ??= "";
    }

    public override string ToString() => string.IsNullOrWhiteSpace(Relationship)
        ? Name
        : $"{Name} · {Relationship}";
}
