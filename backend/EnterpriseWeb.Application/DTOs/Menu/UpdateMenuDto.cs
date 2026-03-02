namespace EnterpriseWeb.Application.DTOs.Menu;

using System.ComponentModel.DataAnnotations;

public record UpdateMenuDto(
    [Required] Guid Id,
    Guid? ParentId,
    [Required] string Title,
    string? Path,
    string? Icon,
    int SortOrder,
    bool IsVisible,
    IEnumerable<string>? RequiredPermissions = null
);

