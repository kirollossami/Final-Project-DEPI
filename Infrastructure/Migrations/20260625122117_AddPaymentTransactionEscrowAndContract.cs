using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionEscrowAndContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ContractId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceivingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HandoverDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DurationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationValue = table.Column<int>(type: "int", nullable: false),
                    OwnerFullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerNationalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StudentFullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StudentNationalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GeneratedPdfUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StudentSignedPdfUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OwnerSignedPdfUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FinalSignedPdfUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsStudentSigned = table.Column<bool>(type: "bit", nullable: false),
                    IsOwnerSigned = table.Column<bool>(type: "bit", nullable: false),
                    StudentSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAdminApproved = table.Column<bool>(type: "bit", nullable: false),
                    AdminUserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdminApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.ContractId);
                    table.ForeignKey(
                        name: "FK_Contracts_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymobOrderId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymobIntentionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymobTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GatewayStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentToken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaymentUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RawResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CallbackSuccess = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CallbackPending = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CallbackFailed = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CallbackProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscrowTransactions",
                columns: table => new
                {
                    EscrowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeldAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedByUserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReleaseTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OwnerPayoutTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OwnerPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OwnerPayoutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlatformFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformFeePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscrowTransactions", x => x.EscrowId);
                    table.ForeignKey(
                        name: "FK_EscrowTransactions_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "ContractId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscrowTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReceipts",
                columns: table => new
                {
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscrowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedToUserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IssuedToRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssuedToName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceiptData = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ReceiptPdfUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsEmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReceipts", x => x.ReceiptId);
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_EscrowTransactions_EscrowId",
                        column: x => x.EscrowId,
                        principalTable: "EscrowTransactions",
                        principalColumn: "EscrowId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "PaymentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b3329416-6bab-4ff6-834e-acbfbc1cec5e", "AQAAAAIAAYagAAAAEH/xl1yuOMef5pdM68/VX6M6qSOfqZtv+kbxvTtMC851b8FkDxZhfnK/An6aJbV0eg==", "8cec72d1-1a7f-4429-a8e2-286c5499777e" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BookingId",
                table: "Contracts",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscrowTransactions_ContractId",
                table: "EscrowTransactions",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowTransactions_PaymentId",
                table: "EscrowTransactions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_EscrowId",
                table: "PaymentReceipts",
                column: "EscrowId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_PaymentId",
                table: "PaymentReceipts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentId",
                table: "PaymentTransactions",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentReceipts");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "EscrowTransactions");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.AlterColumn<string>(
                name: "ContractId",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "739eb7e6-344d-4f52-a7e7-ae5bbd22dc24", "AQAAAAIAAYagAAAAEMNCgvKl5cgfSoXngzQQVt2QWjfFa7EFLDO3UfIpoK5qUa+ZZoku9pLtWyqTyen07A==", "e3ed5410-3299-4705-8f2f-fd94942aaf52" });
        }
    }
}
