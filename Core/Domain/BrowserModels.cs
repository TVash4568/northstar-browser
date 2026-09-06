namespace Newton.Core.Domain;

public readonly record struct ProfileId(Guid Value) { public static ProfileId New() => new(Guid.NewGuid()); public override string ToString() => Value.ToString("N"); }
public readonly record struct WorkspaceId(Guid Value) { public static WorkspaceId New() => new(Guid.NewGuid()); }
public readonly record struct TabId(Guid Value) { public static TabId New() => new(Guid.NewGuid()); }
public readonly record struct TabGroupId(string Value);
public readonly record struct EngineInstanceId(Guid Value);

public sealed class ProfileModel(ProfileId id, string name, bool isPrivate = false)
{
    private readonly List<WorkspaceModel> _workspaces = [];
    public ProfileId Id { get; } = id;
    public string Name { get; set; } = name;
    public bool IsPrivate { get; } = isPrivate;
    public IReadOnlyList<WorkspaceModel> Workspaces => _workspaces;
    public void AddWorkspace(WorkspaceModel workspace)
    {
        if (workspace.ProfileId != Id) throw new InvalidOperationException("Workspace belongs to another profile.");
        _workspaces.Add(workspace);
    }
}

public sealed class WorkspaceModel(WorkspaceId id, ProfileId profileId, string name)
{
    private readonly List<TabModel> _tabs = [];
    public WorkspaceId Id { get; } = id;
    public ProfileId ProfileId { get; } = profileId;
    public string Name { get; set; } = name;
    public IReadOnlyList<TabModel> Tabs => _tabs;
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : Name[..1].ToUpperInvariant();
    public void AddTab(TabModel tab)
    {
        if (tab.WorkspaceId != Id) throw new InvalidOperationException("Tab belongs to another workspace.");
        _tabs.Add(tab);
    }
}

public sealed class TabModel(TabId id, WorkspaceId workspaceId, Uri url)
{
    public TabId Id { get; } = id;
    public Uri Url { get; set; } = url;
    public string Title { get; set; } = "New page";
    public Uri? Favicon { get; set; }
    public bool IsPinned { get; set; }
    public WorkspaceId WorkspaceId { get; set; } = workspaceId;
    public TabGroupId GroupId { get; set; } = new("General");
    public DateTimeOffset LastActive { get; set; } = DateTimeOffset.UtcNow;
    public TabState State { get; set; } = TabState.Active;
    public EngineInstanceId? EngineInstanceId { get; set; }
    public string DisplayTitle => $"{GroupId.Value}  ·  {(Title.Length > 22 ? Title[..22] + "…" : Title)}";
    public override string ToString() => DisplayTitle;
}

public enum TabState { Active, Background, Sleeping, Discarded, Crashed }
