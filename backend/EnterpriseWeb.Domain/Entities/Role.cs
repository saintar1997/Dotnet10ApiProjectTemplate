namespace EnterpriseWeb.Domain.Entities;

public record Role
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<Permission> Permissions { get; init; } = [];
}
