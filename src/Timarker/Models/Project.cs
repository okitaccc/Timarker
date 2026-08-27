namespace Timarker.Models;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? DeadlineAt { get; set; }
    public bool IsCompleted { get; set; }
    public List<ProjectStep> Steps { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public void NormalizeAfterLoad()
    {
        Name ??= "";
        Notes ??= "";
        Steps ??= [];
        foreach (var step in Steps) step.NormalizeAfterLoad();
        Steps = Steps.OrderBy(x => x.Order).ToList();
        for (var i = 0; i < Steps.Count; i++) Steps[i].Order = i + 1;
    }
}

public sealed class ProjectStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public int Order { get; set; }

    public void NormalizeAfterLoad() => Order = Math.Max(0, Order);
}
