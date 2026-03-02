namespace EnterpriseWeb.Application.DTOs.Permission;

public record PermissionDto(
    Guid Id,
    string Code,
    string Module,
    string? Description
);
