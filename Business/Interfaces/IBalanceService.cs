namespace Business.Interfaces;

public interface IBalanceService
{
    /// <summary>
    /// Get balance by user ID
    /// </summary>
    Task<decimal?> GetBalanceByUserIdAsync(string userId);
    
    /// <summary>
    /// Add funds to user's balance
    /// </summary>
    Task AddToBalanceAsync(string userId, string userRole, decimal amount, string reference);
    
    /// <summary>
    /// Deduct funds from user's balance
    /// </summary>
    Task DeductFromBalanceAsync(string userId, string userRole, decimal amount, string reference);
    
    /// <summary>
    /// Transfer funds between users
    /// </summary>
    Task TransferBalanceAsync(string fromUserId, string toUserId, string toUserRole, decimal amount, string reference);
    
    /// <summary>
    /// Get or create balance for user
    /// </summary>
    Task<Domain.Entities.Balance> GetOrCreateBalanceAsync(string userId, string userRole);
}
