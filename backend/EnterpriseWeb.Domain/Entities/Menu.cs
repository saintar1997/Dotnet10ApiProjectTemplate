namespace EnterpriseWeb.Domain.Entities;

public record Menu
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Path { get; init; }
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; }
    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<Menu> Children { get; init; } = [];
    public IReadOnlyList<string> RequiredPermissions { get; init; } = [];
}
