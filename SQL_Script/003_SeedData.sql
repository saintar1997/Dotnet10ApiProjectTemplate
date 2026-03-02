-- ==============================================================================
-- 003_SeedData.sql
-- แทรกข้อมูลเริ่มต้นทั้งหมด: Permissions, Roles, Users, Menus และ Mappings
-- รวมจาก: 002_InitialMockData.sql, 004_SeedRbacMenusAndPermissions.sql,
--         005_MapAdminUserToRoles.sql, 006_SeedUserUpdateDeletePermissions.sql
-- ==============================================================================

USE [EnterpriseWeb_DB];
GO

-- ==============================================================================
-- 1. ประกาศตัวแปรทั้งหมดก่อน (สำหรับ ID ที่ต้องใช้อ้างอิง)
-- ==============================================================================

-- Roles
DECLARE @AdminRoleId UNIQUEIDENTIFIER = NEWID();
DECLARE @StaffRoleId UNIQUEIDENTIFIER = NEWID();

-- Users
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @StaffUserId UNIQUEIDENTIFIER = NEWID();

-- Permissions - Basic (Dashboard, Users)
DECLARE @PermDashboardViewId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermUserViewId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermUserCreateId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermUserUpdateId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermUserDeleteId UNIQUEIDENTIFIER = NEWID();

-- Permissions - RBAC (Roles, Permissions, Menus)
DECLARE @PermRolesViewId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermRolesCreateId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermRolesUpdateId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermRolesDeleteId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermPermissionsViewId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermMenusViewId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermMenusCreateId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermMenusUpdateId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermMenusDeleteId UNIQUEIDENTIFIER = NEWID();

-- Menus - Basic
DECLARE @MenuDashboardId UNIQUEIDENTIFIER = NEWID();
DECLARE @MenuUserManageId UNIQUEIDENTIFIER = NEWID();
DECLARE @MenuUserListId UNIQUEIDENTIFIER = NEWID();

-- Menus - System Settings
DECLARE @SystemSettingsMenuId UNIQUEIDENTIFIER = NEWID();
DECLARE @RolesMenuId UNIQUEIDENTIFIER = NEWID();
DECLARE @PermissionsMenuId UNIQUEIDENTIFIER = NEWID();
DECLARE @MenusManageMenuId UNIQUEIDENTIFIER = NEWID();

-- ==============================================================================
-- 2. Insert Roles (ทีเดียว)
-- ==============================================================================

INSERT INTO [dbo].[Roles] ([Id], [Name], [Description]) VALUES
(@AdminRoleId, 'System Admin', 'สิทธิ์สูงสุดในระบบ'),
(@StaffRoleId, 'General Staff', 'พนักงานทั่วไป');

-- ==============================================================================
-- 3. Insert Users (ทีเดียว)
-- ==============================================================================

INSERT INTO [dbo].[Users] ([Id], [Username], [Email], [PasswordHash]) VALUES
(@AdminUserId, 'admin_user', 'admin@company.com', 'hashed_password_here'),
(@StaffUserId, 'staff_01', 'staff01@company.com', 'hashed_password_here');

-- ==============================================================================
-- 4. Insert Permissions ทั้งหมด (ทีเดียว)
-- ==============================================================================

INSERT INTO [dbo].[Permissions] ([Id], [Code], [Module], [Description]) VALUES
-- Basic Permissions (Dashboard, Users)
(@PermDashboardViewId, 'dashboard:view', 'Dashboard', 'ดูหน้าแดชบอร์ด'),
(@PermUserViewId, 'users:view', 'User Management', 'ดูรายการผู้ใช้งาน'),
(@PermUserCreateId, 'users:create', 'User Management', 'สร้างผู้ใช้งานใหม่'),
(@PermUserUpdateId, 'users:update', 'User Management', 'แก้ไขข้อมูลผู้ใช้งาน'),
(@PermUserDeleteId, 'users:delete', 'User Management', 'ลบผู้ใช้งาน'),
-- RBAC Permissions (Roles, Permissions, Menus)
(@PermRolesViewId, 'roles:view', 'Role Management', 'ดูรายการบทบาท'),
(@PermRolesCreateId, 'roles:create', 'Role Management', 'สร้างบทบาทใหม่'),
(@PermRolesUpdateId, 'roles:update', 'Role Management', 'แก้ไขบทบาท'),
(@PermRolesDeleteId, 'roles:delete', 'Role Management', 'ลบบทบาท'),
(@PermPermissionsViewId, 'permissions:view', 'Permission Management', 'ดูรายการสิทธิ์ระบบ'),
(@PermMenusViewId, 'menus:view', 'Menu Management', 'ดูรายการเมนู'),
(@PermMenusCreateId, 'menus:create', 'Menu Management', 'สร้างเมนูใหม่'),
(@PermMenusUpdateId, 'menus:update', 'Menu Management', 'แก้ไขเมนู'),
(@PermMenusDeleteId, 'menus:delete', 'Menu Management', 'ลบเมนู');

-- ==============================================================================
-- 5. Insert Menus ทั้งหมด (ทีเดียว)
-- ==============================================================================

INSERT INTO [dbo].[Menus] ([Id], [ParentId], [Title], [Path], [Icon], [SortOrder]) VALUES
-- Basic Menus
(@MenuDashboardId, NULL, 'Dashboard', '/dashboard', '📊', 1),
(@MenuUserManageId, NULL, 'User Management', NULL, '👥', 2),
(@MenuUserListId, @MenuUserManageId, 'User List', '/users/list', '📋', 1),
-- System Settings Menus
(@SystemSettingsMenuId, NULL, 'System Settings', NULL, '⚙️', 99),
(@RolesMenuId, @SystemSettingsMenuId, 'Roles', '/roles', '🛡️', 1),
(@PermissionsMenuId, @SystemSettingsMenuId, 'Permissions', '/permissions', '🔑', 2),
(@MenusManageMenuId, @SystemSettingsMenuId, 'Menus Manage', '/menus-manage', '🗂️', 3);

-- ==============================================================================
-- 6. Map User <-> Role (ทีเดียว)
-- ==============================================================================

INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES
(@AdminUserId, @AdminRoleId),
(@StaffUserId, @StaffRoleId);

-- ==============================================================================
-- 7. Map Role <-> Permission (ทีเดียว - Admin ได้ทุกอย่าง)
-- ==============================================================================

INSERT INTO [dbo].[RolePermissions] ([RoleId], [PermissionId]) VALUES
-- Admin ได้รับ Permissions ทั้งหมด
(@AdminRoleId, @PermDashboardViewId),
(@AdminRoleId, @PermUserViewId),
(@AdminRoleId, @PermUserCreateId),
(@AdminRoleId, @PermUserUpdateId),
(@AdminRoleId, @PermUserDeleteId),
(@AdminRoleId, @PermRolesViewId),
(@AdminRoleId, @PermRolesCreateId),
(@AdminRoleId, @PermRolesUpdateId),
(@AdminRoleId, @PermRolesDeleteId),
(@AdminRoleId, @PermPermissionsViewId),
(@AdminRoleId, @PermMenusViewId),
(@AdminRoleId, @PermMenusCreateId),
(@AdminRoleId, @PermMenusUpdateId),
(@AdminRoleId, @PermMenusDeleteId),
-- Staff ได้รับเฉพาะ Dashboard
(@StaffRoleId, @PermDashboardViewId);

-- ==============================================================================
-- 8. Map Menu <-> Permission (ทีเดียว)
-- ==============================================================================

INSERT INTO [dbo].[MenuPermissions] ([MenuId], [PermissionId]) VALUES
-- Dashboard Menu
(@MenuDashboardId, @PermDashboardViewId),
-- User Management Menus
(@MenuUserManageId, @PermUserViewId),
(@MenuUserListId, @PermUserViewId),
-- System Settings Menus
(@SystemSettingsMenuId, @PermRolesViewId),
(@RolesMenuId, @PermRolesViewId),
(@PermissionsMenuId, @PermPermissionsViewId),
(@MenusManageMenuId, @PermMenusViewId);

PRINT 'All seed data has been inserted successfully.';
PRINT 'Summary:';
PRINT '  - Roles: System Admin, General Staff';
PRINT '  - Users: admin_user, staff_01';
PRINT '  - Permissions: 14 permissions (Dashboard, Users, Roles, Permissions, Menus)';
PRINT '  - Menus: 7 menus (Dashboard, User Management, System Settings, etc.)';
PRINT '  - Mappings: User-Role, Role-Permission, Menu-Permission';
GO
