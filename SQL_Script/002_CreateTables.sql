-- ==============================================================================
-- 002_CreateTables.sql
-- สร้าง Tables ทั้งหมดพร้อม Temporal Tables
-- ==============================================================================

USE [EnterpriseWeb_DB];
GO

-- ==============================================================================
-- 1. Create Main Tables
-- ==============================================================================

CREATE TABLE [dbo].[Users] (
    [Id] UNIQUEIDENTIFIER CONSTRAINT [DF_Users_Id] DEFAULT NEWSEQUENTIALID() NOT NULL,
    [Username] VARCHAR(50) NOT NULL,
    [Email] VARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [IsActive] BIT CONSTRAINT [DF_Users_IsActive] DEFAULT 1 NOT NULL,
    [CreatedAt] DATETIME2 CONSTRAINT [DF_Users_CreatedAt] DEFAULT GETUTCDATE() NOT NULL,
    [CreatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Users_Username] UNIQUE NONCLUSTERED ([Username]),
    CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email])
);

CREATE TABLE [dbo].[Roles] (
    [Id] UNIQUEIDENTIFIER CONSTRAINT [DF_Roles_Id] DEFAULT NEWSEQUENTIALID() NOT NULL,
    [Name] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2 CONSTRAINT [DF_Roles_CreatedAt] DEFAULT GETUTCDATE() NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Roles_Name] UNIQUE NONCLUSTERED ([Name])
);

CREATE TABLE [dbo].[Permissions] (
    [Id] UNIQUEIDENTIFIER CONSTRAINT [DF_Permissions_Id] DEFAULT NEWSEQUENTIALID() NOT NULL,
    [Code] VARCHAR(100) NOT NULL, -- เช่น 'users:view', 'users:create'
    [Module] NVARCHAR(50) NOT NULL, -- เช่น 'User Management'
    [Description] NVARCHAR(255) NULL,
    [CreatedAt] DATETIME2 CONSTRAINT [DF_Permissions_CreatedAt] DEFAULT GETUTCDATE() NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Permissions_Code] UNIQUE NONCLUSTERED ([Code])
);

CREATE TABLE [dbo].[Menus] (
    [Id] UNIQUEIDENTIFIER CONSTRAINT [DF_Menus_Id] DEFAULT NEWSEQUENTIALID() NOT NULL,
    [ParentId] UNIQUEIDENTIFIER NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [Path] VARCHAR(255) NULL,
    [Icon] NVARCHAR(50) NULL,  -- รองรับ Unicode (emojis)
    [SortOrder] INT CONSTRAINT [DF_Menus_SortOrder] DEFAULT 0 NOT NULL,
    [IsVisible] BIT CONSTRAINT [DF_Menus_IsVisible] DEFAULT 1 NOT NULL,
    [CreatedAt] DATETIME2 CONSTRAINT [DF_Menus_CreatedAt] DEFAULT GETUTCDATE() NOT NULL,
    CONSTRAINT [PK_Menus] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Menus_Menus_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Menus] ([Id]) ON DELETE NO ACTION
);

-- ==============================================================================
-- 2. Create Mapping Tables (Many-to-Many)
-- ==============================================================================

CREATE TABLE [dbo].[UserRoles] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[RolePermissions] (
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[MenuPermissions] (
    [MenuId] UNIQUEIDENTIFIER NOT NULL,
    [PermissionId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_MenuPermissions] PRIMARY KEY CLUSTERED ([MenuId], [PermissionId]),
    CONSTRAINT [FK_MenuPermissions_Menus] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MenuPermissions_Permissions] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([Id]) ON DELETE CASCADE
);

-- ==============================================================================
-- 3. Create Additional Indexes for Performance
-- ==============================================================================

CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles] ([RoleId]);
CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId] ON [dbo].[RolePermissions] ([PermissionId]);
CREATE NONCLUSTERED INDEX [IX_MenuPermissions_PermissionId] ON [dbo].[MenuPermissions] ([PermissionId]);
CREATE NONCLUSTERED INDEX [IX_Menus_ParentId] ON [dbo].[Menus] ([ParentId]);

-- ==============================================================================
-- 4. SQL Server Temporal Tables (System-Versioned) Setup
-- ==============================================================================

-- 4.1 ตาราง [Users]
ALTER TABLE [dbo].[Users]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[Users]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[UsersHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- 4.2 ตาราง [Roles]
ALTER TABLE [dbo].[Roles]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[Roles]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[RolesHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- 4.3 ตาราง [Permissions]
ALTER TABLE [dbo].[Permissions]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[Permissions]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[PermissionsHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- 4.4 ตาราง [Menus]
ALTER TABLE [dbo].[Menus]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[Menus]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[MenusHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- 4.5 ตาราง [UserRoles]
ALTER TABLE [dbo].[UserRoles]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[UserRoles]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[UserRolesHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- 4.6 ตาราง [RolePermissions]
ALTER TABLE [dbo].[RolePermissions]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[RolePermissions]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[RolePermissionsHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- 4.7 ตาราง [MenuPermissions]
ALTER TABLE [dbo].[MenuPermissions]
ADD
    [SysStartTime] DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL DEFAULT SYSUTCDATETIME(),
    [SysEndTime] DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime]);
GO
ALTER TABLE [dbo].[MenuPermissions]
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[MenuPermissionsHistory], HISTORY_RETENTION_PERIOD = 3 YEARS));
GO

-- ==============================================================================
-- 5. Create Indexes for History Tables (Performance Tuning)
-- ==============================================================================

-- ตารางหลัก (ค้นหาตาม PK และช่วงเวลา)
CREATE NONCLUSTERED INDEX [IX_UsersHistory_Id_Period] ON [dbo].[UsersHistory] ([Id], [SysStartTime], [SysEndTime]);
CREATE NONCLUSTERED INDEX [IX_RolesHistory_Id_Period] ON [dbo].[RolesHistory] ([Id], [SysStartTime], [SysEndTime]);
CREATE NONCLUSTERED INDEX [IX_PermissionsHistory_Id_Period] ON [dbo].[PermissionsHistory] ([Id], [SysStartTime], [SysEndTime]);
CREATE NONCLUSTERED INDEX [IX_MenusHistory_Id_Period] ON [dbo].[MenusHistory] ([Id], [SysStartTime], [SysEndTime]);

-- ตาราง Mapping (ค้นหาตาม Composite PK และช่วงเวลา)
CREATE NONCLUSTERED INDEX [IX_UserRolesHistory_PK_Period] ON [dbo].[UserRolesHistory] ([UserId], [RoleId], [SysStartTime], [SysEndTime]);
CREATE NONCLUSTERED INDEX [IX_RolePermissionsHistory_PK_Period] ON [dbo].[RolePermissionsHistory] ([RoleId], [PermissionId], [SysStartTime], [SysEndTime]);
CREATE NONCLUSTERED INDEX [IX_MenuPermissionsHistory_PK_Period] ON [dbo].[MenuPermissionsHistory] ([MenuId], [PermissionId], [SysStartTime], [SysEndTime]);
GO

PRINT 'All tables and temporal tables have been created successfully.';
GO
