using System.Text.Json;
using Timarker.Models;
using Timarker.Services;

namespace Timarker.Core.Tests;

public sealed class EventStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsIndependentFoldersAndProjects()
    {
        using var directory = new TestDirectory();
        var item = new EventItem { Title = "步骤" };
        var folder = new Folder { Name = "工作", EventIds = [item.Id] };
        var project = new Project { Name = "发布", Steps = [new ProjectStep { EventId = item.Id, Order = 1 }] };
        var store = new EventStore(directory.Path);

        store.Save([item], folders: [folder], projects: [project]);
        var loaded = new EventStore(directory.Path);
        var events = loaded.Load();

        Assert.Equal(item.Id, Assert.Single(events).Id);
        Assert.True(Assert.Single(loaded.Folders).Contains(item.Id));
        Assert.Equal(item.Id, Assert.Single(loaded.Projects).Steps.Single().EventId);
    }

    [Fact]
    public void Load_MigratesLegacyFolderProjectAndStep()
    {
        using var directory = new TestDirectory();
        var folder = new EventItem { Title = "旧收藏夹", IsGroup = true };
        var project = new EventItem { Title = "旧项目", IsProject = true };
        var step = new EventItem { Title = "旧步骤", FolderIds = [folder.Id], ProjectId = project.Id, ProjectOrder = 1 };
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "events.json"), JsonSerializer.Serialize(new[] { folder, project, step }));
        var store = new EventStore(directory.Path);

        var events = store.Load();

        Assert.Equal(step.Id, Assert.Single(events).Id);
        Assert.True(Assert.Single(store.Folders).Contains(step.Id));
        Assert.Equal(step.Id, Assert.Single(store.Projects).Steps.Single().EventId);
    }

    [Fact]
    public void Load_UsesFallbackWhenPrimaryFileIsCorrupt()
    {
        using var directory = new TestDirectory();
        var store = new EventStore(directory.Path);
        store.Save([new EventItem { Title = "第一版" }]);
        store.Save([new EventItem { Title = "第二版" }]);
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "events.json"), "{ invalid json");
        var recovered = new EventStore(directory.Path);

        var events = recovered.Load();

        Assert.Equal("第一版", Assert.Single(events).Title);
        Assert.NotNull(recovered.RecoveryMessage);
    }

    [Fact]
    public void Load_RemovesReferencesToDeletedEvents()
    {
        using var directory = new TestDirectory();
        var missingId = Guid.NewGuid();
        var store = new EventStore(directory.Path);
        store.Save([], folders: [new Folder { Name = "空", EventIds = [missingId] }],
            projects: [new Project { Name = "空", Steps = [new ProjectStep { EventId = missingId, Order = 1 }] }]);
        var loaded = new EventStore(directory.Path);

        loaded.Load();

        Assert.Empty(Assert.Single(loaded.Folders).EventIds);
        Assert.Empty(Assert.Single(loaded.Projects).Steps);
    }

    private sealed class TestDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"timarker-tests-{Guid.NewGuid():N}");
        public TestDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
