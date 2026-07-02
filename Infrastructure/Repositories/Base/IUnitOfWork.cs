using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Base;

public interface IUnitOfWork : IDisposable
{
    IBookingRepository Bookings { get; }
    IPaymentRepository Payments { get; }
    IContractRepository Contracts { get; }
    IPaymentTransactionRepository PaymentTransactions { get; }
    IEscrowTransactionRepository EscrowTransactions { get; }
    IPaymentReceiptRepository PaymentReceipts { get; }
    IPaymentHistoryRepository PaymentHistories { get; }
    INotificationRepository Notifications { get; }
    IStudentRepository Students { get; }
    ILandLordRepository LandLords { get; }
    IHousingUnitRepository HousingUnits { get; }
    IRoomRepository Rooms { get; }
    IBedRepository Beds { get; }
    IReviewRepository Reviews { get; }
    IWishlistRepository Wishlists { get; }
    IComplaintRepository Complaints { get; }
    ICommissionRecordRepository CommissionRecords { get; }
    
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
