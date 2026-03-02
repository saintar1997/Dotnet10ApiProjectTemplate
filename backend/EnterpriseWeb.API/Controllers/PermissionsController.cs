namespace EnterpriseWeb.API.Controllers;

using EnterpriseWeb.Application.DTOs.Permission;
using EnterpriseWeb.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var permissions = await permissionService.GetAllPermissionsAsync();
        return Ok(permissions);
    }
}
