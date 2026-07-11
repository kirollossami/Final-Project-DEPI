-- Pending Migrations for MonsterASP.net Adminer
-- Uses ADD/COPY/DROP instead of sp_rename for compatibility

-- ============ MIGRATION 15: Add SignatureDeadline ============
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SignatureDeadline')
BEGIN
    ALTER TABLE [Contracts] ADD [SignatureDeadline] datetime2 NULL
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentHistories')
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
        [Currency] nvarchar(3) NOT NULL DEFAULT 'EGP',
        [PreviousStatus] nvarchar(100) NOT NULL,
        [NewStatus] nvarchar(100) NOT NULL,
        [MetadataJson] nvarchar(max) NULL,
        [ActorUserId] nvarchar(256) NULL,
        [ActorRole] nvarchar(50) NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PaymentHistories] PRIMARY KEY ([HistoryId])
    )
    CREATE INDEX [IX_PaymentHistories_BookingId] ON [PaymentHistories] ([BookingId])
    CREATE INDEX [IX_PaymentHistories_CreatedAt] ON [PaymentHistories] ([CreatedAt])
    CREATE INDEX [IX_PaymentHistories_PaymentId] ON [PaymentHistories] ([PaymentId])
    CREATE INDEX [IX_PaymentHistories_UserId] ON [PaymentHistories] ([UserId])
END

-- ============ MIGRATION 16: Contract + Escrow column changes ============

-- Drop old Contracts columns
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'GeneratedPdfUrl')
    ALTER TABLE [Contracts] DROP COLUMN [GeneratedPdfUrl]

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'OwnerSignedAt')
    ALTER TABLE [Contracts] DROP COLUMN [OwnerSignedAt]

-- Contracts: StudentSignedPdfUrl -> StudentSignedContractPath
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'StudentSignedPdfUrl')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'StudentSignedContractPath')
BEGIN
    ALTER TABLE [Contracts] ADD [StudentSignedContractPath] nvarchar(2000) NULL
    EXEC('UPDATE [Contracts] SET [StudentSignedContractPath] = [StudentSignedPdfUrl]')
    ALTER TABLE [Contracts] DROP COLUMN [StudentSignedPdfUrl]
END

-- Contracts: OwnerSignedPdfUrl -> OriginalContractPdfPath
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'OwnerSignedPdfUrl')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'OriginalContractPdfPath')
BEGIN
    ALTER TABLE [Contracts] ADD [OriginalContractPdfPath] nvarchar(2000) NULL
    EXEC('UPDATE [Contracts] SET [OriginalContractPdfPath] = [OwnerSignedPdfUrl]')
    ALTER TABLE [Contracts] DROP COLUMN [OwnerSignedPdfUrl]
END

-- Contracts: FinalSignedPdfUrl -> LandlordSignedContractPath
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'FinalSignedPdfUrl')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'LandlordSignedContractPath')
BEGIN
    ALTER TABLE [Contracts] ADD [LandlordSignedContractPath] nvarchar(2000) NULL
    EXEC('UPDATE [Contracts] SET [LandlordSignedContractPath] = [FinalSignedPdfUrl]')
    ALTER TABLE [Contracts] DROP COLUMN [FinalSignedPdfUrl]
END

-- Contracts: SignatureDeadline -> LandlordSignedAt
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'SignatureDeadline')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'LandlordSignedAt')
BEGIN
    ALTER TABLE [Contracts] ADD [LandlordSignedAt] datetime2 NULL
    EXEC('UPDATE [Contracts] SET [LandlordSignedAt] = [SignatureDeadline]')
    ALTER TABLE [Contracts] DROP COLUMN [SignatureDeadline]
END

-- Contracts: IsOwnerSigned -> IsLandlordSigned (THE ONE CAUSING YOUR ERROR)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'IsOwnerSigned')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'IsLandlordSigned')
BEGIN
    ALTER TABLE [Contracts] ADD [IsLandlordSigned] bit NOT NULL DEFAULT 0
    EXEC('UPDATE [Contracts] SET [IsLandlordSigned] = [IsOwnerSigned]')
    ALTER TABLE [Contracts] DROP COLUMN [IsOwnerSigned]
END

-- Add ContractStatus to Contracts
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Contracts') AND name = 'ContractStatus')
BEGIN
    ALTER TABLE [Contracts] ADD [ContractStatus] nvarchar(max) NOT NULL DEFAULT ''
END

-- EscrowTransactions: Make PaymentId nullable
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'PaymentId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EscrowTransactions_Payments_PaymentId')
        ALTER TABLE [EscrowTransactions] DROP CONSTRAINT [FK_EscrowTransactions_Payments_PaymentId]
    ALTER TABLE [EscrowTransactions] ALTER COLUMN [PaymentId] uniqueidentifier NULL
END

-- EscrowTransactions: Make ContractId nullable
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'ContractId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EscrowTransactions_Contracts_ContractId')
        ALTER TABLE [EscrowTransactions] DROP CONSTRAINT [FK_EscrowTransactions_Contracts_ContractId]
    ALTER TABLE [EscrowTransactions] ALTER COLUMN [ContractId] uniqueidentifier NULL
END

-- EscrowTransactions: Add new columns
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'BookingId')
    ALTER TABLE [EscrowTransactions] ADD [BookingId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'LandlordId')
    ALTER TABLE [EscrowTransactions] ADD [LandlordId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'PaymentReference')
    ALTER TABLE [EscrowTransactions] ADD [PaymentReference] nvarchar(500) NOT NULL DEFAULT ''

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'StudentId')
    ALTER TABLE [EscrowTransactions] ADD [StudentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'TransactionType')
    ALTER TABLE [EscrowTransactions] ADD [TransactionType] nvarchar(50) NOT NULL DEFAULT ''

-- EscrowTransactions: HeldAmount -> Amount
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'HeldAmount')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'Amount')
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [Amount] decimal(18,2) NOT NULL DEFAULT 0
    EXEC('UPDATE [EscrowTransactions] SET [Amount] = [HeldAmount]')
    ALTER TABLE [EscrowTransactions] DROP COLUMN [HeldAmount]
END

-- EscrowTransactions: OwnerPayoutTransactionId -> LandlordPayoutTransactionId
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'OwnerPayoutTransactionId')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'LandlordPayoutTransactionId')
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [LandlordPayoutTransactionId] nvarchar(500) NULL
    EXEC('UPDATE [EscrowTransactions] SET [LandlordPayoutTransactionId] = [OwnerPayoutTransactionId]')
    ALTER TABLE [EscrowTransactions] DROP COLUMN [OwnerPayoutTransactionId]
END

-- EscrowTransactions: OwnerPayoutAt -> LandlordPayoutAt
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'OwnerPayoutAt')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'LandlordPayoutAt')
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [LandlordPayoutAt] datetime2 NULL
    EXEC('UPDATE [EscrowTransactions] SET [LandlordPayoutAt] = [OwnerPayoutAt]')
    ALTER TABLE [EscrowTransactions] DROP COLUMN [OwnerPayoutAt]
END

-- EscrowTransactions: OwnerPayoutAmount -> LandlordPayoutAmount
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'OwnerPayoutAmount')
    AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EscrowTransactions') AND name = 'LandlordPayoutAmount')
BEGIN
    ALTER TABLE [EscrowTransactions] ADD [LandlordPayoutAmount] decimal(18,2) NULL
    EXEC('UPDATE [EscrowTransactions] SET [LandlordPayoutAmount] = [OwnerPayoutAmount]')
    ALTER TABLE [EscrowTransactions] DROP COLUMN [OwnerPayoutAmount]
END

-- Create Balances table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Balances')
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
    )
END

-- Create EscrowTransactions index and FK
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EscrowTransactions_BookingId' AND object_id = OBJECT_ID('EscrowTransactions'))
    CREATE INDEX [IX_EscrowTransactions_BookingId] ON [EscrowTransactions] ([BookingId])

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EscrowTransactions_Bookings_BookingId')
    ALTER TABLE [EscrowTransactions] ADD CONSTRAINT [FK_EscrowTransactions_Bookings_BookingId]
        FOREIGN KEY ([BookingId]) REFERENCES [Bookings]([BookingId]) ON DELETE RESTRICT

-- ============ MIGRATION 17: Update booking status enum values ============
UPDATE [Bookings] SET [BookingStatus] = 'PendingPayment' WHERE [BookingStatus] = 'PaymentProcessing'
UPDATE [Bookings] SET [BookingStatus] = 'WaitingForContract' WHERE [BookingStatus] = 'ContractSent'
UPDATE [Bookings] SET [BookingStatus] = 'WaitingForSignatures' WHERE [BookingStatus] = 'ContractUploaded'
UPDATE [Bookings] SET [BookingStatus] = 'WaitingForStudentSignature' WHERE [BookingStatus] = 'LandlordSigned'
UPDATE [Bookings] SET [BookingStatus] = 'WaitingForLandlordSignature' WHERE [BookingStatus] = 'StudentSigned'
UPDATE [Bookings] SET [BookingStatus] = 'WaitingForAdminApproval' WHERE [BookingStatus] = 'BothSigned'
UPDATE [Bookings] SET [BookingStatus] = 'Approved' WHERE [BookingStatus] = 'Completed'
UPDATE [Bookings] SET [BookingStatus] = 'Rejected' WHERE [BookingStatus] = 'Declined'

-- ============ MIGRATION 18: Fix Pending -> PendingPayment ============
UPDATE [Bookings] SET [BookingStatus] = 'PendingPayment' WHERE [BookingStatus] = 'Pending'

-- ============ Record migrations in EF history ============
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260708205705_AddContractSignatureDeadlineAndBookingStatusExpired', '8.0.26')

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260709155746_UpdateContractAndEscrowForManualWorkflow')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260709155746_UpdateContractAndEscrowForManualWorkflow', '8.0.26')

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260709173008_UpdateBookingStatusEnumValues')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260709173008_UpdateBookingStatusEnumValues', '8.0.26')

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260709184747_FixPendingToWaitingForContract')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260709184747_FixPendingToWaitingForContract', '8.0.26')

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260709223036_updateContract')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260709223036_updateContract', '8.0.26')
