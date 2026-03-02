namespace EnterpriseWeb.Application.DTOs.Menu;

using System.ComponentModel.DataAnnotations;

public record CreateMenuDto(
    Guid? ParentId,
    [Required] string Title,
    string? Path,
    string? Icon,
    int SortOrder,
    bool IsVisible,
    IEnumerable<string>? RequiredPermissions = null
);

