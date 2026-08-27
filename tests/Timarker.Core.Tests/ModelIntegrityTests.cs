using Timarker.Models;

namespace Timarker.Core.Tests;

public sealed class ModelIntegrityTests
{
    [Fact]
    public void Folder_DoesNotAddSameEventTwice()
    {
        var eventId = Guid.NewGuid();
        var folder = new Folder();

        folder.Add(eventId);
        folder.Add(eventId);

        Assert.Equal([eventId], folder.EventIds);
    }

    [Fact]
    public void ProjectNormalization_SortsAndRenumbersSteps()
    {
        var first = new ProjectStep { EventId = Guid.NewGuid(), Order = 8 };
        var second = new ProjectStep { EventId = Guid.NewGuid(), Order = 2 };
        var project = new Project { Steps = [first, second] };

        project.NormalizeAfterLoad();

        Assert.Same(second, project.Steps[0]);
        Assert.Equal(1, project.Steps[0].Order);
        Assert.Equal(2, project.Steps[1].Order);
    }

    [Fact]
    public void EventNormalization_RepairsInvalidCollectionsAndSelfProjectReference()
    {
        var item = new EventItem { FolderIds = null!, PersonIds = null!, Occurrences = null! };
        item.ProjectId = item.Id;
        item.ProjectOrder = -4;

        item.NormalizeAfterLoad();

        Assert.Empty(item.FolderIds);
        Assert.Empty(item.PersonIds);
        Assert.Empty(item.Occurrences);
        Assert.Null(item.ProjectId);
        Assert.Equal(0, item.ProjectOrder);
    }
}
