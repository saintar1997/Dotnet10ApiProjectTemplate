namespace EnterpriseWeb.Application.Interfaces;

using EnterpriseWeb.Application.DTOs.Menu;

public interface IMenuService
{
    Task<IEnumerable<MenuDto>> GetAllMenusAsync();
    Task<IEnumerable<MenuDto>> GetMenusForUserAsync(IEnumerable<string> permissionCodes);
    Task<MenuDto?> GetMenuByIdAsync(Guid id);
    Task<Guid> CreateMenuAsync(CreateMenuDto dto);
    Task UpdateMenuAsync(UpdateMenuDto dto);
    Task DeleteMenuAsync(Guid id);
}
