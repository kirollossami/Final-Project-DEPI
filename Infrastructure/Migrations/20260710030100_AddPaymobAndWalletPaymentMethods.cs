using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// Migration to add Paymob and Wallet payment methods to the PaymentMethod enum
    /// 
    /// Current enum values: InstaPay=0, VodafoneCash=1, Cash=2, CreditCard=3, Card=4
    /// New values: Paymob=5, Wallet=6
    /// 
    /// This migration tracks the enum addition. EF Core handles enum mapping automatically.
    /// No direct SQL changes needed as enum values are stored as integers in SQL Server.
    /// </summary>
    public partial class AddPaymobAndWalletPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enum changes are handled by EF Core automatically
            // The PaymentMethod enum now includes: Paymob and Wallet
            // No SQL needed - this migration serves as a version marker
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback would require ensuring no existing records use Paymob (5) or Wallet (6)
            // This is a placeholder for potential rollback logic
        }
    }
}
