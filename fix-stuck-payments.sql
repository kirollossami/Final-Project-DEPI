-- ============================================================
-- FIX: Manually complete 2 stuck payments where Paymob reported
-- success but the webhook never reached the backend.
-- Run this against the StudentHousingDB database.
-- ============================================================
BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────
-- STEP 1: Update PaymentTransaction records → GatewayStatus = Success (2)
-- ─────────────────────────────────────────────────────────────
UPDATE pt
SET pt.GatewayStatus     = 2,          -- Success
    pt.CompletedAt       = GETUTCDATE(),
    pt.CallbackSuccess   = 'true',
    pt.CallbackProcessedAt = GETUTCDATE()
FROM PaymentTransactions pt
WHERE pt.TransactionId IN (
    '7935CC2C-FD26-4478-8849-7F0D4A77711E',   -- 19500 EGP
    '18F8E3BC-D374-4F8A-A131-190DCD6614B8'    -- 9750 EGP
);

-- ─────────────────────────────────────────────────────────────
-- STEP 2: Update Payment records → PaymentStatus = Completed (1)
-- ─────────────────────────────────────────────────────────────
UPDATE p
SET p.PaymentStatus = 1,               -- Completed
    p.CompletedAt   = GETUTCDATE()
FROM Payments p
WHERE p.PaymentId IN (
    'F8350FAC-8EEE-466F-9686-9FE308642C79',   -- 19500 EGP
    '4AD5199B-A8B2-48E9-BCDD-B66725F88133'    -- 9750 EGP
);

-- ─────────────────────────────────────────────────────────────
-- STEP 3: Update Booking records → BookingStatus = WaitingForContract (1)
-- ─────────────────────────────────────────────────────────────
UPDATE b
SET b.BookingStatus = 1,               -- WaitingForContract
    b.UpdatedAt     = GETUTCDATE()
FROM Bookings b
WHERE b.BookingId IN (
    '6AE6E6FE-198D-45E6-A1F7-135C5848B40F',   -- 19500 EGP booking
    'F27E5D05-0CB2-4CE0-96A9-758E35287FA6'    -- 9750 EGP booking
);

-- ─────────────────────────────────────────────────────────────
-- STEP 4: Create EscrowTransaction for each booking
-- ─────────────────────────────────────────────────────────────

-- Escrow for 19500 EGP booking (if not already exists)
IF NOT EXISTS (SELECT 1 FROM EscrowTransactions WHERE PaymentId = 'F8350FAC-8EEE-466F-9686-9FE308642C79')
BEGIN
    INSERT INTO EscrowTransactions
        (EscrowId, BookingId, StudentId, LandlordId, PaymentId, ContractId,
         Amount, Currency, Status, TransactionType, PaymentReference,
         PlatformFee, PlatformFeePercentage, CreatedAt)
    SELECT
        NEWID(),
        b.BookingId,
        b.StudentId,
        hu.LandLordId,
        p.PaymentId,
        NULL,                         -- ContractId: linked later
        p.Amount,
        'EGP',
        0,                            -- Holding
        'Payment',
        pt.PaymobIntentionId,
        p.Amount * 0.05,              -- 5% platform fee
        5.0,
        GETUTCDATE()
    FROM Bookings b
    INNER JOIN Payments p ON p.BookingId = b.BookingId
    INNER JOIN PaymentTransactions pt ON pt.PaymentId = p.PaymentId
    LEFT JOIN HousingUnits hu ON hu.HousingUnitId = b.HousingUnitId
    WHERE p.PaymentId = 'F8350FAC-8EEE-466F-9686-9FE308642C79';
END

-- Escrow for 9750 EGP booking (if not already exists)
IF NOT EXISTS (SELECT 1 FROM EscrowTransactions WHERE PaymentId = '4AD5199B-A8B2-48E9-BCDD-B66725F88133')
BEGIN
    INSERT INTO EscrowTransactions
        (EscrowId, BookingId, StudentId, LandlordId, PaymentId, ContractId,
         Amount, Currency, Status, TransactionType, PaymentReference,
         PlatformFee, PlatformFeePercentage, CreatedAt)
    SELECT
        NEWID(),
        b.BookingId,
        b.StudentId,
        hu.LandLordId,
        p.PaymentId,
        NULL,
        p.Amount,
        'EGP',
        0,                            -- Holding
        'Payment',
        pt.PaymobIntentionId,
        p.Amount * 0.05,
        5.0,
        GETUTCDATE()
    FROM Bookings b
    INNER JOIN Payments p ON p.BookingId = b.BookingId
    INNER JOIN PaymentTransactions pt ON pt.PaymentId = p.PaymentId
    LEFT JOIN HousingUnits hu ON hu.HousingUnitId = b.HousingUnitId
    WHERE p.PaymentId = '4AD5199B-A8B2-48E9-BCDD-B66725F88133';
END

-- ─────────────────────────────────────────────────────────────
-- STEP 5: Credit admin balance (total: 19500 + 9750 = 29250 EGP)
-- ─────────────────────────────────────────────────────────────
-- Find admin user from AspNetUsers
DECLARE @AdminUserId NVARCHAR(450);
SELECT TOP 1 @AdminUserId = u.Id
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE r.Name = 'Admin';

IF @AdminUserId IS NOT NULL
BEGIN
    -- Get or create admin balance
    IF EXISTS (SELECT 1 FROM Balances WHERE UserId = @AdminUserId)
    BEGIN
        UPDATE Balances
        SET AvailableBalance = AvailableBalance + 29250.00,
            TotalReceived    = TotalReceived + 29250.00,
            UpdatedAt        = GETUTCDATE()
        WHERE UserId = @AdminUserId;
    END
    ELSE
    BEGIN
        INSERT INTO Balances
            (BalanceId, UserId, UserRole, AvailableBalance, TotalReceived, TotalPaidOut, Currency, CreatedAt, UpdatedAt)
        VALUES
            (NEWID(), @AdminUserId, 'Admin', 29250.00, 29250.00, 0, 'EGP', GETUTCDATE(), GETUTCDATE());
    END
END

COMMIT TRANSACTION;

-- ─────────────────────────────────────────────────────────────
-- VERIFICATION: Check results
-- ─────────────────────────────────────────────────────────────
SELECT b.BookingId, b.BookingStatus, b.UpdatedAt
FROM Bookings b
WHERE b.BookingId IN (
    '6AE6E6FE-198D-45E6-A1F7-135C5848B40F',
    'F27E5D05-0CB2-4CE0-96A9-758E35287FA6'
);

SELECT p.PaymentId, p.PaymentStatus, p.CompletedAt
FROM Payments p
WHERE p.PaymentId IN (
    'F8350FAC-8EEE-466F-9686-9FE308642C79',
    '4AD5199B-A8B2-48E9-BCDD-B66725F88133'
);

SELECT pt.TransactionId, pt.GatewayStatus, pt.CallbackProcessedAt
FROM PaymentTransactions pt
WHERE pt.TransactionId IN (
    '7935CC2C-FD26-4478-8849-7F0D4A77711E',
    '18F8E3BC-D374-4F8A-A131-190DCD6614B8'
);

SELECT e.EscrowId, e.BookingId, e.Amount, e.Status
FROM EscrowTransactions e
WHERE e.PaymentId IN (
    'F8350FAC-8EEE-466F-9686-9FE308642C79',
    '4AD5199B-A8B2-48E9-BCDD-B66725F88133'
);

SELECT b.AvailableBalance, b.TotalReceived
FROM Balances b
WHERE b.UserId = @AdminUserId;
