using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public interface IPaymentTransactionRepository : IBaseRepository<PaymentTransaction>
{
    Task<PaymentTransaction?> GetByPaymentIdAsync(Guid paymentId);
    Task<PaymentTransaction?> GetByPaymobOrderIdAsync(string orderId);
    Task<PaymentTransaction?> GetByPaymobTransactionIdAsync(string transactionId);
}

public class PaymentTransactionRepository : BaseRepository<PaymentTransaction>, IPaymentTransactionRepository
{
    public PaymentTransactionRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<PaymentTransaction?> GetByPaymentIdAsync(Guid paymentId)
    {
        return await entities
            .Include(pt => pt.Payment)
            .FirstOrDefaultAsync(pt => pt.PaymentId == paymentId);
    }

    public async Task<PaymentTransaction?> GetByPaymobOrderIdAsync(string orderId)
    {
        return await entities
            .FirstOrDefaultAsync(pt => pt.PaymobOrderId == orderId);
    }

    public async Task<PaymentTransaction?> GetByPaymobTransactionIdAsync(string transactionId)
    {
        return await entities
            .FirstOrDefaultAsync(pt => pt.PaymobTransactionId == transactionId);
    }
}
