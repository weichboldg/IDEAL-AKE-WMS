IF OBJECT_ID(N'dbo.UserViewPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserViewPreferences] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [UserId]            INT NOT NULL,
        [ViewKey]           NVARCHAR(50) NOT NULL,
        [SettingsJson]      NVARCHAR(MAX) NOT NULL,
        [CreatedAt]         DATETIME2 NOT NULL,
        [CreatedBy]         NVARCHAR(200) NOT NULL,
        [CreatedByWindows]  NVARCHAR(200) NOT NULL,
        [ModifiedAt]        DATETIME2 NULL,
        [ModifiedBy]        NVARCHAR(200) NULL,
        [ModifiedByWindows] NVARCHAR(200) NULL,
        CONSTRAINT [PK_UserViewPreferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserViewPreferences_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_UserViewPreferences_User_View] UNIQUE ([UserId], [ViewKey])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260410124248_AddUserViewPreferences')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260410124248_AddUserViewPreferences', N'10.0.0');
END
GO
