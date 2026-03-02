namespace EnterpriseWeb.Application.DTOs.Role;

using System.ComponentModel.DataAnnotations;

public record UpdateRoleDto(
    [Required] Guid Id,
    [Required] string Name,
    string? Description,
    IEnumerable<Guid>? PermissionIds
);
