namespace EnterpriseWeb.Application.DTOs.Role;

using EnterpriseWeb.Application.DTOs.Permission;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    IEnumerable<PermissionDto>? Permissions
);
