namespace EnterpriseWeb.Application.Services;

using EnterpriseWeb.Application.DTOs.Menu;
using EnterpriseWeb.Application.Interfaces;
using EnterpriseWeb.Domain.Entities;
using EnterpriseWeb.Domain.Interfaces;

public class MenuService(IMenuRepository menuRepository, IUnitOfWork unitOfWork) : IMenuService
{
    public async Task<IEnumerable<MenuDto>> GetAllMenusAsync()
    {
        var menus = await menuRepository.GetAllAsync();
        return BuildTree(menus, null);
    }

    public async Task<IEnumerable<MenuDto>> GetMenusForUserAsync(IEnumerable<string> permissionCodes)
    {
        var menus = await menuRepository.GetMenusByUserPermissionsAsync(permissionCodes);
        return BuildTree(menus, null);
    }

    public async Task<MenuDto?> GetMenuByIdAsync(Guid id)
    {
        var m = await menuRepository.GetByIdAsync(id);
        if (m == null) return null;

        return new MenuDto(
            m.Id,
            m.ParentId,
            m.Title,
            m.Path,
            m.Icon,
            m.SortOrder,
            m.IsVisible,
            []
        );
    }

    public async Task<Guid> CreateMenuAsync(CreateMenuDto dto)
    {
        var menu = new Menu
        {
            ParentId = dto.ParentId,
            Title = dto.Title,
            Path = dto.Path,
            Icon = dto.Icon,
            SortOrder = dto.SortOrder,
            IsVisible = dto.IsVisible
        };

        unitOfWork.Begin();
        try
        {
            var menuId = await menuRepository.CreateAsync(menu, unitOfWork.Connection, unitOfWork.Transaction!);

            if (dto.RequiredPermissions is not null && dto.RequiredPermissions.Any())
            {
                await menuRepository.UpdateMenuPermissionsAsync(
                    menuId, dto.RequiredPermissions, unitOfWork.Connection, unitOfWork.Transaction!);
            }

            unitOfWork.Commit();
            return menuId;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    public async Task UpdateMenuAsync(UpdateMenuDto dto)
    {
        var menu = await menuRepository.GetByIdAsync(dto.Id);
        if (menu == null) throw new KeyNotFoundException("Menu not found");

        var updated = menu with
        {
            ParentId = dto.ParentId,
            Title = dto.Title,
            Path = dto.Path,
            Icon = dto.Icon,
            SortOrder = dto.SortOrder,
            IsVisible = dto.IsVisible
        };

        unitOfWork.Begin();
        try
        {
            await menuRepository.UpdateAsync(updated, unitOfWork.Connection, unitOfWork.Transaction!);

            if (dto.RequiredPermissions is not null)
            {
                await menuRepository.UpdateMenuPermissionsAsync(
                    dto.Id, dto.RequiredPermissions, unitOfWork.Connection, unitOfWork.Transaction!);
            }

            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    public async Task DeleteMenuAsync(Guid id)
    {
        unitOfWork.Begin();
        try
        {
            await menuRepository.DeleteAsync(id, unitOfWork.Connection, unitOfWork.Transaction!);
            unitOfWork.Commit();
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }

    private static IEnumerable<MenuDto> BuildTree(IEnumerable<Menu> allMenus, Guid? parentId)
    {
        return allMenus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.SortOrder)
            .Select(m => new MenuDto(
                m.Id,
                m.ParentId,
                m.Title,
                m.Path,
                m.Icon,
                m.SortOrder,
                m.IsVisible,
                BuildTree(allMenus, m.Id)
            ));
    }
}
