CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Distributors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(120) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [IsSimulation] bit NOT NULL,
    CONSTRAINT [PK_Distributors] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Genres] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    CONSTRAINT [PK_Genres] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [VipPlans] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Price] decimal(12,2) NOT NULL,
    [DurationDays] int NOT NULL,
    CONSTRAINT [PK_VipPlans] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [DisplayName] nvarchar(80) NOT NULL,
    [DistributorId] int NULL,
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
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUsers_Distributors_DistributorId] FOREIGN KEY ([DistributorId]) REFERENCES [Distributors] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE TABLE [Songs] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(160) NOT NULL,
    [Artist] nvarchar(120) NOT NULL,
    [Description] nvarchar(1500) NOT NULL,
    [DurationSeconds] int NOT NULL,
    [CoverPath] nvarchar(260) NOT NULL,
    [AudioFileName] nvarchar(120) NOT NULL,
    [HighQualityFileName] nvarchar(120) NULL,
    [FlacFileName] nvarchar(120) NULL,
    [GenreId] int NOT NULL,
    [ImportedById] nvarchar(450) NOT NULL,
    [DistributorId] int NULL,
    [ExternalRecordingCode] nvarchar(64) NULL,
    [ExternalReleaseCode] nvarchar(64) NULL,
    [Status] nvarchar(16) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [CoverFileName] nvarchar(120) NULL,
    [RightsHolder] nvarchar(160) NOT NULL,
    [RightsEvidence] nvarchar(2000) NOT NULL,
    [ReviewNote] nvarchar(1000) NULL,
    [ReviewedAt] datetimeoffset NULL,
    [ReviewedById] nvarchar(450) NULL,
    CONSTRAINT [PK_Songs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Songs_AspNetUsers_ImportedById] FOREIGN KEY ([ImportedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Songs_AspNetUsers_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Songs_Distributors_DistributorId] FOREIGN KEY ([DistributorId]) REFERENCES [Distributors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Songs_Genres_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [Genres] ([Id]) ON DELETE NO ACTION
);
GO


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
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE INDEX [IX_AspNetUsers_DistributorId] ON [AspNetUsers] ([DistributorId]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE INDEX [IX_DemoOrders_UserId] ON [DemoOrders] ([UserId]);
GO


CREATE INDEX [IX_DemoOrders_VipPlanId] ON [DemoOrders] ([VipPlanId]);
GO


CREATE UNIQUE INDEX [IX_Distributors_Code] ON [Distributors] ([Code]);
GO


CREATE UNIQUE INDEX [IX_Genres_Name] ON [Genres] ([Name]);
GO


CREATE UNIQUE INDEX [IX_Songs_DistributorId_ExternalRecordingCode] ON [Songs] ([DistributorId], [ExternalRecordingCode]) WHERE [DistributorId] IS NOT NULL AND [ExternalRecordingCode] IS NOT NULL;
GO


CREATE INDEX [IX_Songs_GenreId] ON [Songs] ([GenreId]);
GO


CREATE INDEX [IX_Songs_ImportedById] ON [Songs] ([ImportedById]);
GO


CREATE INDEX [IX_Songs_ReviewedById] ON [Songs] ([ReviewedById]);
GO


CREATE INDEX [IX_Songs_Status_CreatedAt] ON [Songs] ([Status], [CreatedAt]);
GO


CREATE INDEX [IX_VipSubscriptions_UserId_EndsAt] ON [VipSubscriptions] ([UserId], [EndsAt]);
GO


CREATE INDEX [IX_VipSubscriptions_VipPlanId] ON [VipSubscriptions] ([VipPlanId]);
GO


