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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [ProfileImage] nvarchar(500) NULL,
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [LandLords] (
        [LandLordId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NULL,
        [CompanyName] nvarchar(200) NULL,
        [NationalId] nvarchar(20) NOT NULL,
        [PropertyOwnerShipProof] nvarchar(500) NOT NULL,
        [VerificationStatus] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_LandLords] PRIMARY KEY ([LandLordId]),
        CONSTRAINT [FK_LandLords_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [NotificationId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsSeen] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
        CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Students] (
        [StudentId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Gender] nvarchar(max) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [PreferredArea] nvarchar(100) NOT NULL,
        [NationalId] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Students] PRIMARY KEY ([StudentId]),
        CONSTRAINT [FK_Students_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [HousingUnits] (
        [HousingUnitId] uniqueidentifier NOT NULL,
        [LandLordId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [Area] nvarchar(100) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [UnitImageUrl] nvarchar(max) NULL,
        [GenderAllowed] nvarchar(max) NOT NULL,
        [Rules] nvarchar(1000) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [AverageRating] float(3) NULL,
        [ReviewCount] int NOT NULL,
        [Location] nvarchar(500) NOT NULL,
        [NumberOfRooms] int NOT NULL,
        [IsAvailable] bit NOT NULL,
        CONSTRAINT [PK_HousingUnits] PRIMARY KEY ([HousingUnitId]),
        CONSTRAINT [FK_HousingUnits_LandLords_LandLordId] FOREIGN KEY ([LandLordId]) REFERENCES [LandLords] ([LandLordId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Complaints] (
        [ComplaintId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [LandLordId] uniqueidentifier NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Complaints] PRIMARY KEY ([ComplaintId]),
        CONSTRAINT [FK_Complaints_LandLords_LandLordId] FOREIGN KEY ([LandLordId]) REFERENCES [LandLords] ([LandLordId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Complaints_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Reviews] (
        [ReviewId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [HousingUnitId] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [ReviewDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
        CONSTRAINT [FK_Reviews_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [RoomId] uniqueidentifier NOT NULL,
        [HousingUnitId] uniqueidentifier NOT NULL,
        [RoomType] nvarchar(max) NOT NULL,
        [RoomImageUrl] nvarchar(max) NULL,
        [NumberOfBeds] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [IsAvailable] bit NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([RoomId]),
        CONSTRAINT [FK_Rooms_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Wishlists] (
        [WishlistId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [HousingUnitId] uniqueidentifier NOT NULL,
        [AddedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([WishlistId]),
        CONSTRAINT [FK_Wishlists_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Wishlists_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookings] (
        [BookingId] uniqueidentifier NOT NULL,
        [StudentId] uniqueidentifier NOT NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [TotalPrice] decimal(18,2) NOT NULL,
        [BookingStatus] nvarchar(max) NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingId]),
        CONSTRAINT [FK_Bookings_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([StudentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [PaymentId] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [TransactionId] nvarchar(100) NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
        CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] ON;
    EXEC(N'INSERT INTO [AspNetRoles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
    VALUES (N''1'', NULL, N''Admin'', N''ADMIN''),
    (N''2'', NULL, N''Student'', N''STUDENT''),
    (N''3'', NULL, N''LandLord'', N''LANDLORD'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[AspNetRoles]'))
        SET IDENTITY_INSERT [AspNetRoles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_RoomId] ON [Bookings] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_StudentId] ON [Bookings] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Complaints_LandLordId] ON [Complaints] ([LandLordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Complaints_StudentId] ON [Complaints] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_HousingUnits_LandLordId] ON [HousingUnits] ([LandLordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LandLords_UserId] ON [LandLords] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reviews_HousingUnitId] ON [Reviews] ([HousingUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reviews_StudentId] ON [Reviews] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Rooms_HousingUnitId] ON [Rooms] ([HousingUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Students_UserId] ON [Students] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Wishlists_HousingUnitId] ON [Wishlists] ([HousingUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Wishlists_StudentId] ON [Wishlists] ([StudentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427143330_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427143330_InitialCreate', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428221738_AddRefreshTokenTable'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [IsRevoked] bit NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428221738_AddRefreshTokenTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428221738_AddRefreshTokenTable', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430164910_AddGoogleLoginFields'
)
BEGIN
    ALTER TABLE [Students] ADD [IsOnboardingComplete] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430164910_AddGoogleLoginFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [GoogleId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430164910_AddGoogleLoginFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsGoogleUser] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430164910_AddGoogleLoginFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430164910_AddGoogleLoginFields', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430200303_AddTwoFactorAuthFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [TwoFactorSecret] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430200303_AddTwoFactorAuthFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430200303_AddTwoFactorAuthFields', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    ALTER TABLE [Students] ADD [FacultyName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    ALTER TABLE [Students] ADD [UniversityEmail] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    ALTER TABLE [Students] ADD [UniversityIdCardPath] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    ALTER TABLE [Students] ADD [UniversityName] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    ALTER TABLE [Students] ADD [UniversityVerificationStatus] nvarchar(20) NOT NULL DEFAULT N'NotSubmitted';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    ALTER TABLE [Complaints] ADD [Title] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    CREATE TABLE [CommissionRecords] (
        [CommissionRecordId] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [Rate] decimal(5,4) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CommissionRecords] PRIMARY KEY ([CommissionRecordId]),
        CONSTRAINT [FK_CommissionRecords_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    UPDATE Complaints SET Status = 'Open' WHERE Status = 'Pending'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    UPDATE Complaints SET Status = 'InInvestigation' WHERE Status = 'InProgress'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'ConcurrencyStamp', N'Email', N'EmailConfirmed', N'GoogleId', N'IsActive', N'IsDeleted', N'IsGoogleUser', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'ProfileImage', N'SecurityStamp', N'TwoFactorEnabled', N'TwoFactorSecret', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
        SET IDENTITY_INSERT [AspNetUsers] ON;
    EXEC(N'INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed], [GoogleId], [IsActive], [IsDeleted], [IsGoogleUser], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [ProfileImage], [SecurityStamp], [TwoFactorEnabled], [TwoFactorSecret], [UserName])
    VALUES (N''admin-user-id-001'', 0, N''b7ddcc98-739c-4603-8036-9ada26897501'', N''admin@studenthousing.com'', CAST(1 AS bit), NULL, CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), NULL, N''ADMIN@STUDENTHOUSING.COM'', N''ADMIN@STUDENTHOUSING.COM'', N''AQAAAAIAAYagAAAAEOPkfFC9o7HdqTZXFHQPMZT7dhYmogH+V5cG0rbND+QklAb5/AYDr5+QdwOu7o+w0w=='', NULL, CAST(0 AS bit), NULL, N''1e1faff9-0f47-40a1-95c1-765462e1df3a'', CAST(0 AS bit), NULL, N''admin@studenthousing.com'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'ConcurrencyStamp', N'Email', N'EmailConfirmed', N'GoogleId', N'IsActive', N'IsDeleted', N'IsGoogleUser', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'ProfileImage', N'SecurityStamp', N'TwoFactorEnabled', N'TwoFactorSecret', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
        SET IDENTITY_INSERT [AspNetUsers] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId') AND [object_id] = OBJECT_ID(N'[AspNetUserRoles]'))
        SET IDENTITY_INSERT [AspNetUserRoles] ON;
    EXEC(N'INSERT INTO [AspNetUserRoles] ([RoleId], [UserId])
    VALUES (N''1'', N''admin-user-id-001'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId') AND [object_id] = OBJECT_ID(N'[AspNetUserRoles]'))
        SET IDENTITY_INSERT [AspNetUserRoles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CommissionRecords_BookingId] ON [CommissionRecords] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260617160119_AddUniversityVerificationAndCommissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260617160119_AddUniversityVerificationAndCommissions', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Complaints] DROP CONSTRAINT [FK_Complaints_LandLords_LandLordId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    EXEC sp_rename N'[Complaints].[LandLordId]', N'HousingUnitId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    EXEC sp_rename N'[Complaints].[IX_Complaints_LandLordId]', N'IX_Complaints_HousingUnitId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Rooms] ADD [Capacity] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Rooms] ADD [CurrentOccupancy] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Rooms] ADD [PricePerMonth] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [LandLords] ADD [IsVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HousingUnits]') AND [c].[name] = N'UnitImageUrl');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [HousingUnits] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [HousingUnits] ALTER COLUMN [UnitImageUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [HousingUnits] ADD [VideoUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Bookings] ADD [ContractId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Bookings] ADD [ContractPdfUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    CREATE TABLE [UnitImages] (
        [UnitImageId] uniqueidentifier NOT NULL,
        [HousingUnitId] uniqueidentifier NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UnitImages] PRIMARY KEY ([UnitImageId]),
        CONSTRAINT [FK_UnitImages_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''9ef14dda-cd62-4b7d-a81f-904dc3268d87'', [PasswordHash] = N''AQAAAAIAAYagAAAAEFFqrN0wfsWgnt5YMkDgOnl96uRYkHUtqWMfQc959A3Y41Nn7lPgchgnzqjlhQ6IOg=='', [SecurityStamp] = N''c651695f-12aa-4fca-a9ed-7b9092f278fb''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    CREATE INDEX [IX_UnitImages_HousingUnitId] ON [UnitImages] ([HousingUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    ALTER TABLE [Complaints] ADD CONSTRAINT [FK_Complaints_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619011927_AddLandlordAndHousingUnitUpdate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619011927_AddLandlordAndHousingUnitUpdate', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Rooms] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Rooms] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Rooms] ADD [UpdatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [HousingUnits] ADD [BaseMonthlyPrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [HousingUnits] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [HousingUnits] ADD [UpdatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'RoomId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Bookings] ALTER COLUMN [RoomId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD [BedId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD [BookingType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD [HousingUnitId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD [UpdatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    CREATE TABLE [Beds] (
        [BedId] uniqueidentifier NOT NULL,
        [RoomId] uniqueidentifier NOT NULL,
        [BedNumber] nvarchar(50) NOT NULL,
        [IsAvailable] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Beds] PRIMARY KEY ([BedId]),
        CONSTRAINT [FK_Beds_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''f55e7a6f-982d-446a-aba0-bedc7fb3ca2d'', [PasswordHash] = N''AQAAAAIAAYagAAAAECDbhemZoX7ynk6BZCJTkhFr7wvinLCNjtuyhpCUywHv/OaGdQay8fGYQNaYHE4Eyg=='', [SecurityStamp] = N''c0f40086-dded-483f-a25c-f58deb2eab14''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    CREATE INDEX [IX_Bookings_BedId] ON [Bookings] ([BedId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    CREATE INDEX [IX_Bookings_HousingUnitId] ON [Bookings] ([HousingUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    CREATE INDEX [IX_Beds_RoomId] ON [Beds] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Beds_BedId] FOREIGN KEY ([BedId]) REFERENCES [Beds] ([BedId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619234114_AddHierarchicalBookingSupport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619234114_AddHierarchicalBookingSupport', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620000643_AddIsOccupiedToBed'
)
BEGIN
    ALTER TABLE [Beds] ADD [IsOccupied] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620000643_AddIsOccupiedToBed'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''013b7be7-470b-4572-848b-40e67a7e9a9e'', [PasswordHash] = N''AQAAAAIAAYagAAAAENGwDqamJDfdgud9HisfW4Fxpwq7dPVDdy6gA83kQppj0aivgzekB5mx+eQr4FaGvQ=='', [SecurityStamp] = N''85b21b4e-b16f-4235-8eac-6301e5341d24''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620000643_AddIsOccupiedToBed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620000643_AddIsOccupiedToBed', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620225857_AddLandLordVerificationFields'
)
BEGIN
    ALTER TABLE [LandLords] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620225857_AddLandLordVerificationFields'
)
BEGIN
    ALTER TABLE [LandLords] ADD [HousingUnitDocumentationUrl] nvarchar(500) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620225857_AddLandLordVerificationFields'
)
BEGIN
    ALTER TABLE [LandLords] ADD [NationalIdImageUrl] nvarchar(500) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620225857_AddLandLordVerificationFields'
)
BEGIN
    ALTER TABLE [LandLords] ADD [UpdatedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620225857_AddLandLordVerificationFields'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''484339af-7b09-49e5-9504-d5e80896fde3'', [PasswordHash] = N''AQAAAAIAAYagAAAAEH3fcC04/lzteoyKTBKqZv9Q3/ZBe54vONX1FO/ZlQn8QU9snNgBj6iVx44bI8iMpg=='', [SecurityStamp] = N''2ab862e3-63c3-4397-8453-9f09d2c4ae2b''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620225857_AddLandLordVerificationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620225857_AddLandLordVerificationFields', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    ALTER TABLE [Payments] ADD [ClientSecret] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    ALTER TABLE [Payments] ADD [CompletedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    ALTER TABLE [Payments] ADD [StripePaymentIntentId] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    ALTER TABLE [HousingUnits] ADD [Latitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    ALTER TABLE [HousingUnits] ADD [Longitude] float NOT NULL DEFAULT 0.0E0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    CREATE TABLE [Conversations] (
        [ConversationId] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [StudentUserId] nvarchar(450) NOT NULL,
        [LandLordUserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([ConversationId]),
        CONSTRAINT [FK_Conversations_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    CREATE TABLE [Messages] (
        [MessageId] uniqueidentifier NOT NULL,
        [ConversationId] uniqueidentifier NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [IsRead] bit NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]),
        CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([ConversationId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''7bcc41a6-f248-479d-8e93-fd8249b12eb3'', [PasswordHash] = N''AQAAAAIAAYagAAAAEEb3Oq5X/71DetOKpGq9JsJoJ1OkIgrJwjP1vaWb07vVM4cjkbXvuTnpxDHe4t9lPQ=='', [SecurityStamp] = N''7ad24c00-db39-41ab-bebf-db1cb1f6fa85''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Conversations_BookingId] ON [Conversations] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    CREATE INDEX [IX_Messages_ConversationId_SentAt] ON [Messages] ([ConversationId], [SentAt] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151347_AddLatLongToHousingUnit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622151347_AddLatLongToHousingUnit', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151413_AddConversationsAndMessages'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''824197b3-393f-498e-ac1e-87149880f30c'', [PasswordHash] = N''AQAAAAIAAYagAAAAEP1EW/ahq30NGp6Zv+nH2JlB1R25wmydyJDu5hDIFxWueEYcs64KU0eHaOfdTZ3OIQ=='', [SecurityStamp] = N''0ee8af06-ac3b-499d-9e63-2ee2f7c0fc53''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151413_AddConversationsAndMessages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622151413_AddConversationsAndMessages', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151434_AddStripePaymentFieldsAndBookingStatus'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''b4ed0518-9310-41a6-8f07-f5ceecf6d91b'', [PasswordHash] = N''AQAAAAIAAYagAAAAEHEQ6uwWDcfl6xxljbETHHwieuusgEBmxHBtF7/BjMycBUr8L4U8z2KdpSRdIWks8Q=='', [SecurityStamp] = N''748665fa-15ca-4399-baae-63e1b1b33d92''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260622151434_AddStripePaymentFieldsAndBookingStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260622151434_AddStripePaymentFieldsAndBookingStatus', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    DROP INDEX [IX_Conversations_BookingId] ON [Conversations];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Conversations]') AND [c].[name] = N'BookingId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Conversations] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Conversations] ALTER COLUMN [BookingId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    ALTER TABLE [Conversations] ADD [HousingUnitId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''739eb7e6-344d-4f52-a7e7-ae5bbd22dc24'', [PasswordHash] = N''AQAAAAIAAYagAAAAEMNCgvKl5cgfSoXngzQQVt2QWjfFa7EFLDO3UfIpoK5qUa+ZZoku9pLtWyqTyen07A=='', [SecurityStamp] = N''e3ed5410-3299-4705-8f2f-fd94942aaf52''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Conversations_BookingId] ON [Conversations] ([BookingId]) WHERE [BookingId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    CREATE INDEX [IX_Conversations_HousingUnitId] ON [Conversations] ([HousingUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    ALTER TABLE [Conversations] ADD CONSTRAINT [FK_Conversations_HousingUnits_HousingUnitId] FOREIGN KEY ([HousingUnitId]) REFERENCES [HousingUnits] ([HousingUnitId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624164436_AddPreBookingConversation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260624164436_AddPreBookingConversation', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'ContractId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Bookings] ALTER COLUMN [ContractId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE TABLE [Contracts] (
        [ContractId] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [ContractNumber] nvarchar(100) NOT NULL,
        [ReceivingDate] datetime2 NOT NULL,
        [HandoverDate] datetime2 NOT NULL,
        [FinalPrice] decimal(18,2) NOT NULL,
        [DurationType] nvarchar(max) NOT NULL,
        [DurationValue] int NOT NULL,
        [OwnerFullName] nvarchar(500) NOT NULL,
        [OwnerNationalId] nvarchar(100) NOT NULL,
        [StudentFullName] nvarchar(500) NOT NULL,
        [StudentNationalId] nvarchar(100) NOT NULL,
        [GeneratedPdfUrl] nvarchar(2000) NOT NULL,
        [StudentSignedPdfUrl] nvarchar(2000) NULL,
        [OwnerSignedPdfUrl] nvarchar(2000) NULL,
        [FinalSignedPdfUrl] nvarchar(2000) NULL,
        [IsStudentSigned] bit NOT NULL,
        [IsOwnerSigned] bit NOT NULL,
        [StudentSignedAt] datetime2 NULL,
        [OwnerSignedAt] datetime2 NULL,
        [IsAdminApproved] bit NOT NULL,
        [AdminUserId] nvarchar(500) NULL,
        [AdminApprovedAt] datetime2 NULL,
        [AdminNotes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Contracts] PRIMARY KEY ([ContractId]),
        CONSTRAINT [FK_Contracts_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [TransactionId] uniqueidentifier NOT NULL,
        [PaymentId] uniqueidentifier NOT NULL,
        [PaymobOrderId] nvarchar(500) NOT NULL,
        [PaymobIntentionId] nvarchar(500) NOT NULL,
        [PaymobTransactionId] nvarchar(500) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [GatewayStatus] nvarchar(max) NOT NULL,
        [PaymentToken] nvarchar(1000) NULL,
        [PaymentUrl] nvarchar(2000) NULL,
        [RawResponse] nvarchar(max) NULL,
        [CallbackSuccess] nvarchar(500) NULL,
        [CallbackPending] nvarchar(500) NULL,
        [CallbackFailed] nvarchar(500) NULL,
        [CallbackProcessedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([TransactionId]),
        CONSTRAINT [FK_PaymentTransactions_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE TABLE [EscrowTransactions] (
        [EscrowId] uniqueidentifier NOT NULL,
        [PaymentId] uniqueidentifier NOT NULL,
        [ContractId] uniqueidentifier NOT NULL,
        [HeldAmount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ReleasedAt] datetime2 NULL,
        [ReleasedByUserId] nvarchar(500) NULL,
        [ReleaseTransactionId] nvarchar(500) NULL,
        [ReleaseNotes] nvarchar(1000) NULL,
        [RefundedAt] datetime2 NULL,
        [RefundTransactionId] nvarchar(500) NULL,
        [RefundReason] nvarchar(1000) NULL,
        [OwnerPayoutTransactionId] nvarchar(500) NULL,
        [OwnerPayoutAmount] decimal(18,2) NULL,
        [OwnerPayoutAt] datetime2 NULL,
        [PlatformFee] decimal(18,2) NOT NULL,
        [PlatformFeePercentage] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_EscrowTransactions] PRIMARY KEY ([EscrowId]),
        CONSTRAINT [FK_EscrowTransactions_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([ContractId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EscrowTransactions_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE TABLE [PaymentReceipts] (
        [ReceiptId] uniqueidentifier NOT NULL,
        [PaymentId] uniqueidentifier NOT NULL,
        [EscrowId] uniqueidentifier NOT NULL,
        [ReceiptNumber] nvarchar(100) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [IssuedToUserId] nvarchar(500) NOT NULL,
        [IssuedToRole] nvarchar(50) NOT NULL,
        [IssuedToName] nvarchar(500) NOT NULL,
        [TransactionReference] nvarchar(500) NOT NULL,
        [PaymentMethod] nvarchar(100) NOT NULL,
        [ReceiptData] nvarchar(max) NOT NULL,
        [ReceiptPdfUrl] nvarchar(2000) NOT NULL,
        [IsEmailSent] bit NOT NULL,
        [EmailSentAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PaymentReceipts] PRIMARY KEY ([ReceiptId]),
        CONSTRAINT [FK_PaymentReceipts_EscrowTransactions_EscrowId] FOREIGN KEY ([EscrowId]) REFERENCES [EscrowTransactions] ([EscrowId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentReceipts_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''b3329416-6bab-4ff6-834e-acbfbc1cec5e'', [PasswordHash] = N''AQAAAAIAAYagAAAAEH/xl1yuOMef5pdM68/VX6M6qSOfqZtv+kbxvTtMC851b8FkDxZhfnK/An6aJbV0eg=='', [SecurityStamp] = N''8cec72d1-1a7f-4429-a8e2-286c5499777e''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contracts_BookingId] ON [Contracts] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE INDEX [IX_EscrowTransactions_ContractId] ON [EscrowTransactions] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE INDEX [IX_EscrowTransactions_PaymentId] ON [EscrowTransactions] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE INDEX [IX_PaymentReceipts_EscrowId] ON [PaymentReceipts] ([EscrowId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE INDEX [IX_PaymentReceipts_PaymentId] ON [PaymentReceipts] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    CREATE INDEX [IX_PaymentTransactions_PaymentId] ON [PaymentTransactions] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625122117_AddPaymentTransactionEscrowAndContract'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625122117_AddPaymentTransactionEscrowAndContract', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    ALTER TABLE [Contracts] ADD [SignatureDeadline] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE TABLE [PaymentHistories] (
        [HistoryId] uniqueidentifier NOT NULL,
        [PaymentId] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NULL,
        [EscrowTransactionId] uniqueidentifier NULL,
        [UserId] nvarchar(256) NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL DEFAULT N'EGP',
        [PreviousStatus] nvarchar(100) NOT NULL,
        [NewStatus] nvarchar(100) NOT NULL,
        [MetadataJson] nvarchar(max) NULL,
        [ActorUserId] nvarchar(256) NULL,
        [ActorRole] nvarchar(50) NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_PaymentHistories] PRIMARY KEY ([HistoryId]),
        CONSTRAINT [FK_PaymentHistories_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentHistories_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE SET NULL,
        CONSTRAINT [FK_PaymentHistories_EscrowTransactions_EscrowTransactionId] FOREIGN KEY ([EscrowTransactionId]) REFERENCES [EscrowTransactions] ([EscrowId]) ON DELETE SET NULL,
        CONSTRAINT [FK_PaymentHistories_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''519659f7-8f35-49d0-8f11-e4c34ce36a42'', [PasswordHash] = N''AQAAAAIAAYagAAAAEOt1rKn2JjVWhIZLXhZahdlCt7XJh2tIqWLKBIdEUVyq3nIPMDZ6KQUNjzoS6L128w=='', [SecurityStamp] = N''3f0cfdb5-c6a2-4bcc-a4bd-05921fa5aa58''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_BookingId] ON [PaymentHistories] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_BookingId_CreatedAt] ON [PaymentHistories] ([BookingId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_CreatedAt] ON [PaymentHistories] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_EscrowTransactionId] ON [PaymentHistories] ([EscrowTransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_PaymentId] ON [PaymentHistories] ([PaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_UserId] ON [PaymentHistories] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    CREATE INDEX [IX_PaymentHistories_UserId_CreatedAt] ON [PaymentHistories] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Contracts]') AND [c].[name] = N'GeneratedPdfUrl');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Contracts] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Contracts] DROP COLUMN [GeneratedPdfUrl];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Contracts]') AND [c].[name] = N'OwnerSignedAt');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Contracts] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Contracts] DROP COLUMN [OwnerSignedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[EscrowTransactions].[OwnerPayoutTransactionId]', N'LandlordPayoutTransactionId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[EscrowTransactions].[OwnerPayoutAt]', N'LandlordPayoutAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[EscrowTransactions].[OwnerPayoutAmount]', N'LandlordPayoutAmount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[EscrowTransactions].[HeldAmount]', N'Amount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[Contracts].[StudentSignedPdfUrl]', N'StudentSignedContractPath', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[Contracts].[SignatureDeadline]', N'LandlordSignedAt', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[Contracts].[OwnerSignedPdfUrl]', N'OriginalContractPdfPath', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[Contracts].[IsOwnerSigned]', N'IsLandlordSigned', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC sp_rename N'[Contracts].[FinalSignedPdfUrl]', N'LandlordSignedContractPath', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EscrowTransactions]') AND [c].[name] = N'PaymentId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [EscrowTransactions] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [EscrowTransactions] ALTER COLUMN [PaymentId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EscrowTransactions]') AND [c].[name] = N'ContractId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [EscrowTransactions] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [EscrowTransactions] ALTER COLUMN [ContractId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [BookingId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [LandlordId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [PaymentReference] nvarchar(500) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [StudentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [TransactionType] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [Contracts] ADD [ContractStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    CREATE TABLE [Balances] (
        [BalanceId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [UserRole] nvarchar(max) NOT NULL,
        [AvailableBalance] decimal(18,2) NOT NULL,
        [TotalReceived] decimal(18,2) NOT NULL,
        [TotalPaidOut] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Balances] PRIMARY KEY ([BalanceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''6ec522b6-9667-42e5-8865-65f5e50dd61b'', [PasswordHash] = N''AQAAAAIAAYagAAAAEH+EcBYy8C+VuPEELzhqHNLTSyeFcXKTI8g8o37sHRlNrFTpSWxkMCmMgGZfGZxJOg=='', [SecurityStamp] = N''ceb1ec6e-c735-47f8-b5a4-98b7d1691904''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    CREATE INDEX [IX_EscrowTransactions_BookingId] ON [EscrowTransactions] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    ALTER TABLE [EscrowTransactions] ADD CONSTRAINT [FK_EscrowTransactions_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709155746_UpdateContractAndEscrowForManualWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709155746_UpdateContractAndEscrowForManualWorkflow', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709160825_FixPaymentHistoryUserIdLength'
)
BEGIN
    DROP INDEX [IX_PaymentHistories_UserId] ON [PaymentHistories];
    DROP INDEX [IX_PaymentHistories_UserId_CreatedAt] ON [PaymentHistories];
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaymentHistories]') AND [c].[name] = N'UserId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [PaymentHistories] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [PaymentHistories] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
    CREATE INDEX [IX_PaymentHistories_UserId] ON [PaymentHistories] ([UserId]);
    CREATE INDEX [IX_PaymentHistories_UserId_CreatedAt] ON [PaymentHistories] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709160825_FixPaymentHistoryUserIdLength'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''cf228a4d-bd4d-4087-9408-5f9627f64786'', [PasswordHash] = N''AQAAAAIAAYagAAAAED4fV/dOTj+OYhfoG5vIfBXAO2H6/q7OJMCwB84tOddhtqT5ale2CjGTXC/13/xH5Q=='', [SecurityStamp] = N''44d5fe36-37c4-44bc-9081-b19230a24916''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709160825_FixPaymentHistoryUserIdLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709160825_FixPaymentHistoryUserIdLength', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'PendingPayment' 
                      WHERE BookingStatus = 'PaymentProcessing'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'WaitingForContract' 
                      WHERE BookingStatus = 'ContractSent'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'WaitingForSignatures' 
                      WHERE BookingStatus = 'ContractUploaded'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'WaitingForStudentSignature' 
                      WHERE BookingStatus = 'LandlordSigned'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'WaitingForLandlordSignature' 
                      WHERE BookingStatus = 'StudentSigned'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'WaitingForAdminApproval' 
                      WHERE BookingStatus = 'BothSigned'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'Approved' 
                      WHERE BookingStatus = 'Completed'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'Rejected' 
                      WHERE BookingStatus = 'Declined'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''a1e07e6c-9db6-43f1-bdfa-52969310a3c5'', [PasswordHash] = N''AQAAAAIAAYagAAAAEJq3JVLj9foYHwd/cBYb9iK/2UwHbhafOggMKwmp0cFSDz0E0r9qgpFdauvuloGk5w=='', [SecurityStamp] = N''f29969db-8b3e-4f8d-8913-8f4f438f4272''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709173008_UpdateBookingStatusEnumValues'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709173008_UpdateBookingStatusEnumValues', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709184747_FixPendingToWaitingForContract'
)
BEGIN
    UPDATE Bookings 
                      SET BookingStatus = 'PendingPayment' 
                      WHERE BookingStatus = 'Pending'
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709184747_FixPendingToWaitingForContract'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''e761d59b-ce87-4326-bccb-385ad5563c45'', [PasswordHash] = N''AQAAAAIAAYagAAAAEDYXmBGLnSHi8g5QOn3gFh7Vde4jXW71BeZwNjGKYeg/Eg/6Uj2TMfgHZc9k9dK7ow=='', [SecurityStamp] = N''c03e1cd3-56f7-4b9d-9630-1f15d0d9d78d''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709184747_FixPendingToWaitingForContract'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709184747_FixPendingToWaitingForContract', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709202312_FixPaymentReceiptEscrowIdNullable'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaymentReceipts]') AND [c].[name] = N'EscrowId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [PaymentReceipts] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [PaymentReceipts] ALTER COLUMN [EscrowId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709202312_FixPaymentReceiptEscrowIdNullable'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaymentReceipts]') AND [c].[name] = N'CreatedAt');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [PaymentReceipts] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [PaymentReceipts] ADD DEFAULT (GETUTCDATE()) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709202312_FixPaymentReceiptEscrowIdNullable'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''d0d3e149-dc9a-41a2-9257-9ae80229e7f9'', [PasswordHash] = N''AQAAAAIAAYagAAAAEOtQBgdDegqmQOVHw0Cui1IROJVfJPr3RClNcYCzyzac+oZH0JlrTsVlwY/IcQoJDQ=='', [SecurityStamp] = N''b463da22-a985-4310-bfab-301463db1cc0''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709202312_FixPaymentReceiptEscrowIdNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709202312_FixPaymentReceiptEscrowIdNullable', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709214231_AddPaymentTransactionCreatedAtDefault'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaymentTransactions]') AND [c].[name] = N'CreatedAt');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [PaymentTransactions] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [PaymentTransactions] ADD DEFAULT (GETUTCDATE()) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709214231_AddPaymentTransactionCreatedAtDefault'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''4b2e014f-61d5-4479-89ec-ad25a94e5f2e'', [PasswordHash] = N''AQAAAAIAAYagAAAAEDNVXh8QuG7Rq0Qi38FN8alhSOwF1K8dnunR26+qOfNPOFiqwMHOcXr9s2TwqP6Brw=='', [SecurityStamp] = N''b6f8f493-d553-4c0f-93af-89342884e6c8''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709214231_AddPaymentTransactionCreatedAtDefault'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709214231_AddPaymentTransactionCreatedAtDefault', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709214511_AddPaymentDateDefault'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'PaymentDate');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [Payments] ADD DEFAULT (GETUTCDATE()) FOR [PaymentDate];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709214511_AddPaymentDateDefault'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''757bd6ac-143b-45a5-a387-7ec14727b5c3'', [PasswordHash] = N''AQAAAAIAAYagAAAAECLFCvX0b/EEj10aBvvkFaJcHPRss0ytzJ1VPQqTSUkmLIxX6pzj62sUXNSgxCEJoA=='', [SecurityStamp] = N''b9ea0ecb-cb82-4439-b36e-e4088540787e''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709214511_AddPaymentDateDefault'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709214511_AddPaymentDateDefault', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709220558_AddClientSecretToPaymentTransaction'
)
BEGIN
    ALTER TABLE [PaymentTransactions] ADD [ClientSecret] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709220558_AddClientSecretToPaymentTransaction'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''84224261-44f3-4da7-ac72-fe8706488b92'', [PasswordHash] = N''AQAAAAIAAYagAAAAEObcBLBUMvz66BoMHmBX3HWNEX3aidneZhWPn0dZSnJClpPkUlX0OVhJg3N8D/v7vQ=='', [SecurityStamp] = N''96e297b1-ae0f-457e-8356-3bb28a0bffad''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709220558_AddClientSecretToPaymentTransaction'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709220558_AddClientSecretToPaymentTransaction', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709223036_updateContract'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''4273711f-d28d-418c-b548-fea5e2704507'', [PasswordHash] = N''AQAAAAIAAYagAAAAEMyRL1Kazt8InRAGx2HuW8LCRSYvBprqJhXxGgulxQuqEuYoJ9RunaZgqu+oW/fVEg=='', [SecurityStamp] = N''6ef36639-54f4-4fe1-b0c1-b97874afca39''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709223036_updateContract'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709223036_updateContract', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709225251_AddBalanceTable'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''004dedf6-f28f-4e57-9fa0-4593ff8b9429'', [PasswordHash] = N''AQAAAAIAAYagAAAAEP/HrfKRvj1MYGcGHMNLLVZDfA4aWIB0Q9/3dIKlq3C5NQbZ3CHebWbkpbZqCVKWAw=='', [SecurityStamp] = N''3240bb6d-d417-4add-99f1-7b2b102cd85d''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709225251_AddBalanceTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709225251_AddBalanceTable', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709230407_CreateBalanceTable'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''aaf8eb86-2e96-49f7-94e3-6c9b39a476df'', [PasswordHash] = N''AQAAAAIAAYagAAAAEN/iXtf56uJXOjF3RsfJbvQA0lt/GorNxnvfBAsWRKkXaiaaMRXTViWfJ0oYnx7a4g=='', [SecurityStamp] = N''773c36da-29b8-417d-8063-97eba5110af1''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709230407_CreateBalanceTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709230407_CreateBalanceTable', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710035438_AddPaymobNumericOrderId'
)
BEGIN
    ALTER TABLE [PaymentTransactions] ADD [PaymobNumericOrderId] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710035438_AddPaymobNumericOrderId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710035438_AddPaymobNumericOrderId', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710163414_FixUnitImageUrlToMax'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HousingUnits]') AND [c].[name] = N'UnitImageUrl');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [HousingUnits] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [HousingUnits] ALTER COLUMN [UnitImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710163414_FixUnitImageUrlToMax'
)
BEGIN
    EXEC(N'UPDATE [AspNetUsers] SET [ConcurrencyStamp] = N''83a35259-60fd-4a18-aa96-cd9d16624956'', [PasswordHash] = N''AQAAAAIAAYagAAAAEAsTC8hx4SCUIj56bMB1eojE92QQrnyMbrp6baZ2YP+2Wnc5VhRofWzLcTJYPGhGbg=='', [SecurityStamp] = N''df915d6c-f8fd-4e48-b57d-4e9403b12fd8''
    WHERE [Id] = N''admin-user-id-001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710163414_FixUnitImageUrlToMax'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710163414_FixUnitImageUrlToMax', N'8.0.26');
END;
GO

COMMIT;
GO

