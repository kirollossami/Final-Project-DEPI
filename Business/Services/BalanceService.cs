using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Services;

public class BalanceService : IBalanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BalanceService> _logger;

    public BalanceService(IUnitOfWork unitOfWork, ILogger<BalanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<decimal?> GetBalanceByUserIdAsync(string userId)
    {
        var balance = await _unitOfWork.Balances.GetAll()
            .FirstOrDefaultAsync(b => b.UserId == userId);
        return balance?.AvailableBalance;
    }

    public async Task<Balance?> GetFullBalanceByUserIdAsync(string userId)
    {
        return await _unitOfWork.Balances.GetAll()
            .FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<IEnumerable<Balance>> GetAllBalancesAsync()
    {
        return await _unitOfWork.Balances.GetAll().ToListAsync();
    }

    public async Task AddToBalanceAsync(string userId, string userRole, decimal amount, string reference)
    {
        var balance = await GetOrCreateBalanceAsync(userId, userRole);
        
        balance.AvailableBalance += amount;
        balance.TotalReceived += amount;
        balance.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Balances.Update(balance);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Added {amount} {balance.Currency} to balance for user {userId}. Reference: {reference}");
    }

    public async Task DeductFromBalanceAsync(string userId, string userRole, decimal amount, string reference)
    {
        var balance = await GetOrCreateBalanceAsync(userId, userRole);
        
        if (balance.AvailableBalance < amount)
        {
            throw new InvalidOperationException($"Insufficient balance. Available: {balance.AvailableBalance}, Required: {amount}");
        }
        
        balance.AvailableBalance -= amount;
        balance.TotalPaidOut += amount;
        balance.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Balances.Update(balance);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Deducted {amount} {balance.Currency} from balance for user {userId}. Reference: {reference}");
    }

    public async Task TransferBalanceAsync(string fromUserId, string toUserId, string toUserRole, decimal amount, string reference)
    {
        // Deduct from source
        await DeductFromBalanceAsync(fromUserId, "Admin", amount, reference);
        
        // Add to destination
        await AddToBalanceAsync(toUserId, toUserRole, amount, reference);

        _logger.LogInformation($"Transferred {amount} from {fromUserId} to {toUserId}. Reference: {reference}");
    }

    public async Task<Balance> GetOrCreateBalanceAsync(string userId, string userRole)
    {
        var balance = await _unitOfWork.Balances.GetAll()
            .FirstOrDefaultAsync(b => b.UserId == userId);

        if (balance == null)
        {
            balance = new Balance
            {
                BalanceId = Guid.NewGuid(),
                UserId = userId,
                UserRole = userRole,
                AvailableBalance = 0,
                TotalReceived = 0,
                TotalPaidOut = 0,
                Currency = "EGP",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Balances.Insert(balance);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Created new balance for user {userId} with role {userRole}");
        }

        return balance;
    }
}
