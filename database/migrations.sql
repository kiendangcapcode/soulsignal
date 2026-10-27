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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(80) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [Genres] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        CONSTRAINT [PK_Genres] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [VipPlans] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        [Price] decimal(12,2) NOT NULL,
        [DurationDays] int NOT NULL,
        CONSTRAINT [PK_VipPlans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [Songs] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(160) NOT NULL,
        [Artist] nvarchar(120) NOT NULL,
        [Description] nvarchar(1500) NOT NULL,
        [DurationSeconds] int NOT NULL,
        [CoverPath] nvarchar(260) NOT NULL,
        [AudioFileName] nvarchar(120) NOT NULL,
        [FlacFileName] nvarchar(120) NULL,
        [GenreId] int NOT NULL,
        [PublisherId] nvarchar(450) NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Songs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Songs_AspNetUsers_PublisherId] FOREIGN KEY ([PublisherId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Songs_Genres_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [Genres] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [DemoOrders] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [VipPlanId] int NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        CONSTRAINT [PK_DemoOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DemoOrders_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DemoOrders_VipPlans_VipPlanId] FOREIGN KEY ([VipPlanId]) REFERENCES [VipPlans] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE TABLE [VipSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [VipPlanId] int NOT NULL,
        [StartsAt] datetimeoffset NOT NULL,
        [EndsAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_VipSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VipSubscriptions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_VipSubscriptions_VipPlans_VipPlanId] FOREIGN KEY ([VipPlanId]) REFERENCES [VipPlans] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_DemoOrders_UserId] ON [DemoOrders] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_DemoOrders_VipPlanId] ON [DemoOrders] ([VipPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Genres_Name] ON [Genres] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_Songs_GenreId] ON [Songs] ([GenreId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_Songs_PublisherId] ON [Songs] ([PublisherId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_Songs_Status_CreatedAt] ON [Songs] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_VipSubscriptions_UserId_EndsAt] ON [VipSubscriptions] ([UserId], [EndsAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    CREATE INDEX [IX_VipSubscriptions_VipPlanId] ON [VipSubscriptions] ([VipPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027020445_InitialWeek1'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027020445_InitialWeek1', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD [CoverFileName] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD [ReviewNote] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD [ReviewedAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD [ReviewedById] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD [RightsEvidence] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD [RightsHolder] nvarchar(160) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    CREATE TABLE [PublisherApplications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [OrganizationName] nvarchar(120) NOT NULL,
        [RepresentativeName] nvarchar(80) NOT NULL,
        [ContactEmail] nvarchar(256) NOT NULL,
        [Website] nvarchar(300) NULL,
        [Introduction] nvarchar(1500) NOT NULL,
        [RightsStatement] nvarchar(2000) NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ReviewedAt] datetimeoffset NULL,
        [ReviewedById] nvarchar(450) NULL,
        [ReviewNote] nvarchar(1000) NULL,
        CONSTRAINT [PK_PublisherApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PublisherApplications_AspNetUsers_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PublisherApplications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    CREATE INDEX [IX_Songs_ReviewedById] ON [Songs] ([ReviewedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    CREATE INDEX [IX_PublisherApplications_ReviewedById] ON [PublisherApplications] ([ReviewedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PublisherApplications_UserId] ON [PublisherApplications] ([UserId]) WHERE [Status] = N''Pending''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    ALTER TABLE [Songs] ADD CONSTRAINT [FK_Songs_AspNetUsers_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027030508_DistributorSimulation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027030508_DistributorSimulation', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    ALTER TABLE [Songs] DROP CONSTRAINT [FK_Songs_AspNetUsers_PublisherId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    DROP TABLE [PublisherApplications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    EXEC sp_rename N'[Songs].[PublisherId]', N'ImportedById', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    EXEC sp_rename N'[Songs].[IX_Songs_PublisherId]', N'IX_Songs_ImportedById', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    ALTER TABLE [Songs] ADD [DistributorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    ALTER TABLE [Songs] ADD [ExternalRecordingCode] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    ALTER TABLE [Songs] ADD [ExternalReleaseCode] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    CREATE TABLE [Distributors] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IsSimulation] bit NOT NULL,
        CONSTRAINT [PK_Distributors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Songs_DistributorId_ExternalRecordingCode] ON [Songs] ([DistributorId], [ExternalRecordingCode]) WHERE [DistributorId] IS NOT NULL AND [ExternalRecordingCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Distributors_Code] ON [Distributors] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    ALTER TABLE [Songs] ADD CONSTRAINT [FK_Songs_AspNetUsers_ImportedById] FOREIGN KEY ([ImportedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    ALTER TABLE [Songs] ADD CONSTRAINT [FK_Songs_Distributors_DistributorId] FOREIGN KEY ([DistributorId]) REFERENCES [Distributors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027031358_AdminOnlyDistribution'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027031358_AdminOnlyDistribution', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027043255_DistributorAccounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027043255_DistributorAccounts', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027043420_LinkDistributorAccounts'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DistributorId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027043420_LinkDistributorAccounts'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_DistributorId] ON [AspNetUsers] ([DistributorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027043420_LinkDistributorAccounts'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Distributors_DistributorId] FOREIGN KEY ([DistributorId]) REFERENCES [Distributors] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027043420_LinkDistributorAccounts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027043420_LinkDistributorAccounts', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027045424_FlacAudioRenditions'
)
BEGIN
    ALTER TABLE [Songs] ADD [HighQualityFileName] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027045424_FlacAudioRenditions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027045424_FlacAudioRenditions', N'10.0.14');
END;

COMMIT;
GO

