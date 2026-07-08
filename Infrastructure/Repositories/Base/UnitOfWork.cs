using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Base;

public class UnitOfWork : IUnitOfWork
{
    private readonly StudentHousingDBContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    public UnitOfWork(StudentHousingDBContext context)
    {
        _context = context;
    }

    private IBookingRepository? _bookings;
    private IPaymentRepository? _payments;
    private IContractRepository? _contracts;
    private IPaymentTransactionRepository? _paymentTransactions;
    private IEscrowTransactionRepository? _escrowTransactions;
    private IPaymentReceiptRepository? _paymentReceipts;
    private IPaymentHistoryRepository? _paymentHistories;
    private INotificationRepository? _notifications;
    private IStudentRepository? _students;
    private ILandLordRepository? _landLords;
    private IHousingUnitRepository? _housingUnits;
    private IRoomRepository? _rooms;
    private IBedRepository? _beds;
    private IReviewRepository? _reviews;
    private IWishlistRepository? _wishlists;
    private IComplaintRepository? _complaints;
    private ICommissionRecordRepository? _commissionRecords;

    public IBookingRepository Bookings => _bookings ??= new BookingRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IContractRepository Contracts => _contracts ??= new ContractRepository(_context);
    public IPaymentTransactionRepository PaymentTransactions => _paymentTransactions ??= new PaymentTransactionRepository(_context);
    public IEscrowTransactionRepository EscrowTransactions => _escrowTransactions ??= new EscrowTransactionRepository(_context);
    public IPaymentReceiptRepository PaymentReceipts => _paymentReceipts ??= new PaymentReceiptRepository(_context);
    public IPaymentHistoryRepository PaymentHistories => _paymentHistories ??= new PaymentHistoryRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public IStudentRepository Students => _students ??= new StudentRepository(_context);
    public ILandLordRepository LandLords => _landLords ??= new LandLordRepository(_context);
    public IHousingUnitRepository HousingUnits => _housingUnits ??= new HousingUnitRepository(_context);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(_context);
    public IBedRepository Beds => _beds ??= new BedRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
    public IWishlistRepository Wishlists => _wishlists ??= new WishlistRepository(_context);
    public IComplaintRepository Complaints => _complaints ??= new ComplaintRepository(_context);
    public ICommissionRecordRepository CommissionRecords => _commissionRecords ??= new CommissionRecordRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync();
        return _transaction;
    }

    /// <summary>
    /// Executes the given operation inside a retriable transaction, compatible with
    /// SqlServerRetryingExecutionStrategy (EnableRetryOnFailure).
    /// Use this instead of BeginTransactionAsync() to avoid the
    /// "does not support user-initiated transactions" error.
    /// </summary>
    public Task ExecuteInTransactionAsync(Func<Task> operation)
        => ExecuteInTransactionAsync(async () => { await operation(); return true; });

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        // CreateExecutionStrategy returns a SqlServerRetryingExecutionStrategy.
        // We must wrap the entire transaction (begin + work + commit) inside
        // the strategy's Execute call so retries replay the full transaction.
        var strategy = _context.Database.CreateExecutionStrategy();

        T result = default!;
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                result = await operation();
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
        return result;
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
