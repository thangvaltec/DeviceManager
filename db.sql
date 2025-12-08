IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [admin_users] (
    [id] int NOT NULL IDENTITY,
    [username] nvarchar(max) NOT NULL,
    [password_hash] nvarchar(max) NOT NULL,
    [role] nvarchar(max) NOT NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_admin_users] PRIMARY KEY ([id])
);
GO

CREATE TABLE [device_logs] (
    [Id] int NOT NULL IDENTITY,
    [SerialNo] nvarchar(max) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_device_logs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [devices] (
    [Id] int NOT NULL IDENTITY,
    [SerialNo] nvarchar(max) NOT NULL,
    [DeviceName] nvarchar(max) NOT NULL,
    [AuthMode] int NOT NULL,
    [IsActive] bit NOT NULL,
    [DelFlg] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_devices] PRIMARY KEY ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'created_at', N'password_hash', N'role', N'username') AND [object_id] = OBJECT_ID(N'[admin_users]'))
    SET IDENTITY_INSERT [admin_users] ON;
INSERT INTO [admin_users] ([id], [created_at], [password_hash], [role], [username])
VALUES (1, '2025-12-05T00:00:00.0000000Z', N'39f8485ae66793496c7f4e437acfa60d3905653ea01ca155cf1b5d05446f3702', N'super_admin', N'admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'created_at', N'password_hash', N'role', N'username') AND [object_id] = OBJECT_ID(N'[admin_users]'))
    SET IDENTITY_INSERT [admin_users] OFF;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251208033307_InitClean', N'8.0.10');
GO

COMMIT;
GO

