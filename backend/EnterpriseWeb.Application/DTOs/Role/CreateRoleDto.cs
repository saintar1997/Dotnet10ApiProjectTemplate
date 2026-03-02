namespace EnterpriseWeb.Application.DTOs.Role;

using System.ComponentModel.DataAnnotations;

public record CreateRoleDto(
    [Required] string Name,
    string? Description,
    IEnumerable<Guid>? PermissionIds
);
