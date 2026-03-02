namespace EnterpriseWeb.API.Controllers;

using EnterpriseWeb.Application.DTOs.User;
using EnterpriseWeb.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "users:view")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "users:view")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user is null)
            return NotFound(new { message = $"User with id '{id}' not found." });

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = "users:create")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var id = await userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "users:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { message = "ID mismatch." });

        var existingUser = await userService.GetUserByIdAsync(id);
        if (existingUser is null)
            return NotFound(new { message = $"User with id '{id}' not found." });

        await userService.UpdateUserAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "users:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existingUser = await userService.GetUserByIdAsync(id);
        if (existingUser is null)
            return NotFound(new { message = $"User with id '{id}' not found." });

        await userService.DeleteUserAsync(id);
        return NoContent();
    }
}
