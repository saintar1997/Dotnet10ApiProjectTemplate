namespace EnterpriseWeb.API.Controllers;

using EnterpriseWeb.Application.DTOs.Menu;
using EnterpriseWeb.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenusController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenus()
    {
        var permissionCodes = User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value);

        var menus = await menuService.GetMenusForUserAsync(permissionCodes);
        return Ok(menus);
    }

    [HttpGet("all")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(IEnumerable<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await menuService.GetAllMenusAsync();
        return Ok(menus);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(MenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var menu = await menuService.GetMenuByIdAsync(id);
        if (menu == null) return NotFound();
        return Ok(menu);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
    {
        var newId = await menuService.CreateMenuAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = newId }, newId);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMenuDto dto)
    {
        if (id != dto.Id) return BadRequest("ID mismatch");

        try
        {
            await menuService.UpdateMenuAsync(dto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await menuService.DeleteMenuAsync(id);
        return NoContent();
    }
}
