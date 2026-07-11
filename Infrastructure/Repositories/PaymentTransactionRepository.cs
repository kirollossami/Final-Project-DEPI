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
    Task<PaymentTransaction?> GetByPaymobIntentionIdAsync(string intentionId);
    Task<PaymentTransaction?> GetByPaymobTransactionIdAsync(string transactionId);
    Task<PaymentTransaction?> GetByClientSecretAsync(string clientSecret);
    /// <summary>Match by PaymentToken (= client_secret stored at initiation time).</summary>
    Task<PaymentTransaction?> GetByPaymentTokenAsync(string paymentToken);
    /// <summary>Match by either PaymobOrderId or PaymobIntentionId in one query.</summary>
    Task<PaymentTransaction?> GetByOrderOrIntentionIdAsync(string id);
    /// <summary>Match by the numeric order ID Paymob sends in GET callback ?order=563150958.</summary>
    Task<PaymentTransaction?> GetByNumericOrderIdAsync(string numericOrderId);
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

    public async Task<PaymentTransaction?> GetByPaymobIntentionIdAsync(string intentionId)
    {
        return await entities
            .FirstOrDefaultAsync(pt => pt.PaymobIntentionId == intentionId);
    }

    public async Task<PaymentTransaction?> GetByPaymobTransactionIdAsync(string transactionId)
    {
        return await entities
            .FirstOrDefaultAsync(pt => pt.PaymobTransactionId == transactionId);
    }

    // Match by ClientSecret OR PaymentToken (both can hold the Paymob client_secret)
    public async Task<PaymentTransaction?> GetByClientSecretAsync(string clientSecret)
    {
        return await entities
            .FirstOrDefaultAsync(pt =>
                pt.ClientSecret == clientSecret ||
                pt.PaymentToken == clientSecret);
    }

    // Match by PaymentToken field
    public async Task<PaymentTransaction?> GetByPaymentTokenAsync(string paymentToken)
    {
        return await entities
            .FirstOrDefaultAsync(pt =>
                pt.PaymentToken == paymentToken ||
                pt.ClientSecret == paymentToken);
    }

    // Match by either PaymobOrderId or PaymobIntentionId — handles the case
    // where Paymob sends back the numeric order ID but we stored the intention ID
    // (or vice-versa depending on which API version was used).
    public async Task<PaymentTransaction?> GetByOrderOrIntentionIdAsync(string id)
    {
        return await entities
            .FirstOrDefaultAsync(pt =>
                pt.PaymobOrderId == id ||
                pt.PaymobIntentionId == id);
    }

    // Match by the numeric order ID Paymob sends in the GET redirect:
    // GET /callback?id=493478206&order=563150958  ← "order" maps to this field
    public async Task<PaymentTransaction?> GetByNumericOrderIdAsync(string numericOrderId)
    {
        return await entities
            .FirstOrDefaultAsync(pt => pt.PaymobNumericOrderId == numericOrderId);
    }
}
