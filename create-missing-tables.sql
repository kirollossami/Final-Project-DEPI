-- ============================================================
-- CREATE MISSING TABLES
-- These tables exist in the EF Core model snapshot but were
-- never actually created because the migrations only had
-- UpdateData (seed) without CreateTable.
-- ============================================================

-- 1. Balances table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Balances')
BEGIN
    CREATE TABLE [dbo].[Balances] (
        [BalanceId]        UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [UserId]           NVARCHAR(MAX)    NOT NULL,
        [UserRole]         NVARCHAR(MAX)    NOT NULL,
        [AvailableBalance] DECIMAL(18,2)    NOT NULL DEFAULT 0,
        [TotalReceived]    DECIMAL(18,2)    NOT NULL DEFAULT 0,
        [TotalPaidOut]     DECIMAL(18,2)    NOT NULL DEFAULT 0,
        [Currency]         NVARCHAR(MAX)    NOT NULL DEFAULT 'EGP',
        [UpdatedAt]        DATETIME2        NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedAt]        DATETIME2        NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Balances] PRIMARY KEY ([BalanceId])
    );
    PRINT 'Table [Balances] created.';
END
ELSE
    PRINT 'Table [Balances] already exists.';

-- 2. PaymentHistories table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentHistories')
BEGIN
    CREATE TABLE [dbo].[PaymentHistories] (
        [HistoryId]          UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [PaymentId]          UNIQUEIDENTIFIER NOT NULL,
        [BookingId]          UNIQUEIDENTIFIER NULL,
        [EscrowTransactionId] UNIQUEIDENTIFIER NULL,
        [UserId]             NVARCHAR(450)    NOT NULL,
        [EventType]          NVARCHAR(100)    NOT NULL,
        [Description]        NVARCHAR(500)    NOT NULL,
        [Amount]             DECIMAL(18,2)    NOT NULL,
        [Currency]           NVARCHAR(3)      NOT NULL DEFAULT 'EGP',
        [PreviousStatus]     NVARCHAR(100)    NOT NULL,
        [NewStatus]          NVARCHAR(100)    NOT NULL,
        [ActorUserId]        NVARCHAR(256)    NULL,
        [ActorRole]          NVARCHAR(50)     NULL,
        [IpAddress]          NVARCHAR(50)     NULL,
        [UserAgent]          NVARCHAR(MAX)    NULL,
        [MetadataJson]       NVARCHAR(MAX)    NULL,
        [CreatedAt]          DATETIME2        NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_PaymentHistories] PRIMARY KEY ([HistoryId]),
        CONSTRAINT [FK_PaymentHistories_Payments_PaymentId]
            FOREIGN KEY ([PaymentId]) REFERENCES [dbo].[Payments]([PaymentId]) ON DELETE RESTRICT,
        CONSTRAINT [FK_PaymentHistories_Bookings_BookingId]
            FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([BookingId]) ON DELETE SET NULL,
        CONSTRAINT [FK_PaymentHistories_EscrowTransactions_EscrowTransactionId]
            FOREIGN KEY ([EscrowTransactionId]) REFERENCES [dbo].[EscrowTransactions]([EscrowId]) ON DELETE SET NULL,
        CONSTRAINT [FK_PaymentHistories_AspNetUsers_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE RESTRICT
    );

    CREATE INDEX [IX_PaymentHistories_BookingId] ON [dbo].[PaymentHistories] ([BookingId]);
    CREATE INDEX [IX_PaymentHistories_CreatedAt] ON [dbo].[PaymentHistories] ([CreatedAt]);
    CREATE INDEX [IX_PaymentHistories_EscrowTransactionId] ON [dbo].[PaymentHistories] ([EscrowTransactionId]);
    CREATE INDEX [IX_PaymentHistories_PaymentId] ON [dbo].[PaymentHistories] ([PaymentId]);
    CREATE INDEX [IX_PaymentHistories_UserId] ON [dbo].[PaymentHistories] ([UserId]);
    CREATE INDEX [IX_PaymentHistories_BookingId_CreatedAt] ON [dbo].[PaymentHistories] ([BookingId], [CreatedAt]);
    CREATE INDEX [IX_PaymentHistories_UserId_CreatedAt] ON [dbo].[PaymentHistories] ([UserId], [CreatedAt]);

    PRINT 'Table [PaymentHistories] created.';
END
ELSE
    PRINT 'Table [PaymentHistories] already exists.';

-- 3. Clean up the stuck payment from the failed sync attempt
-- The EscrowTransaction was already inserted but the transaction rolled back.
-- The PaymentTransaction and Payment are still Pending.
-- Delete the orphaned EscrowTransaction if it exists (it was inserted but then the transaction failed)
DELETE FROM [dbo].[EscrowTransactions]
WHERE [EscrowId] = '87f99525-6525-48b6-b2ac-85912eb0b202';
