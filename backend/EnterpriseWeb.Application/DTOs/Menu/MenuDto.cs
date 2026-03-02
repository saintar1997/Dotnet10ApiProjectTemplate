namespace EnterpriseWeb.Application.DTOs.Menu;

public record MenuDto(
    Guid Id,
    Guid? ParentId,
    string Title,
    string? Path,
    string? Icon,
    int SortOrder,
    bool IsVisible,
    IEnumerable<MenuDto> Children
);
