using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateContractAndEscrowForManualWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedPdfUrl",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "OwnerSignedAt",
                table: "Contracts");

            migrationBuilder.RenameColumn(
                name: "OwnerPayoutTransactionId",
                table: "EscrowTransactions",
                newName: "LandlordPayoutTransactionId");

            migrationBuilder.RenameColumn(
                name: "OwnerPayoutAt",
                table: "EscrowTransactions",
                newName: "LandlordPayoutAt");

            migrationBuilder.RenameColumn(
                name: "OwnerPayoutAmount",
                table: "EscrowTransactions",
                newName: "LandlordPayoutAmount");

            migrationBuilder.RenameColumn(
                name: "HeldAmount",
                table: "EscrowTransactions",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "StudentSignedPdfUrl",
                table: "Contracts",
                newName: "StudentSignedContractPath");

            migrationBuilder.RenameColumn(
                name: "SignatureDeadline",
                table: "Contracts",
                newName: "LandlordSignedAt");

            migrationBuilder.RenameColumn(
                name: "OwnerSignedPdfUrl",
                table: "Contracts",
                newName: "OriginalContractPdfPath");

            migrationBuilder.RenameColumn(
                name: "IsOwnerSigned",
                table: "Contracts",
                newName: "IsLandlordSigned");

            migrationBuilder.RenameColumn(
                name: "FinalSignedPdfUrl",
                table: "Contracts",
                newName: "LandlordSignedContractPath");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LandlordId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "EscrowTransactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TransactionType",
                table: "EscrowTransactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContractStatus",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Balances",
                columns: table => new
                {
                    BalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalReceived = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPaidOut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Balances", x => x.BalanceId);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "6ec522b6-9667-42e5-8865-65f5e50dd61b", "AQAAAAIAAYagAAAAEH+EcBYy8C+VuPEELzhqHNLTSyeFcXKTI8g8o37sHRlNrFTpSWxkMCmMgGZfGZxJOg==", "ceb1ec6e-c735-47f8-b5a4-98b7d1691904" });

            migrationBuilder.CreateIndex(
                name: "IX_EscrowTransactions_BookingId",
                table: "EscrowTransactions",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_EscrowTransactions_Bookings_BookingId",
                table: "EscrowTransactions",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EscrowTransactions_Bookings_BookingId",
                table: "EscrowTransactions");

            migrationBuilder.DropTable(
                name: "Balances");

            migrationBuilder.DropIndex(
                name: "IX_EscrowTransactions_BookingId",
                table: "EscrowTransactions");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "EscrowTransactions");

            migrationBuilder.DropColumn(
                name: "LandlordId",
                table: "EscrowTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "EscrowTransactions");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "EscrowTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionType",
                table: "EscrowTransactions");

            migrationBuilder.DropColumn(
                name: "ContractStatus",
                table: "Contracts");

            migrationBuilder.RenameColumn(
                name: "LandlordPayoutTransactionId",
                table: "EscrowTransactions",
                newName: "OwnerPayoutTransactionId");

            migrationBuilder.RenameColumn(
                name: "LandlordPayoutAt",
                table: "EscrowTransactions",
                newName: "OwnerPayoutAt");

            migrationBuilder.RenameColumn(
                name: "LandlordPayoutAmount",
                table: "EscrowTransactions",
                newName: "OwnerPayoutAmount");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "EscrowTransactions",
                newName: "HeldAmount");

            migrationBuilder.RenameColumn(
                name: "StudentSignedContractPath",
                table: "Contracts",
                newName: "StudentSignedPdfUrl");

            migrationBuilder.RenameColumn(
                name: "OriginalContractPdfPath",
                table: "Contracts",
                newName: "OwnerSignedPdfUrl");

            migrationBuilder.RenameColumn(
                name: "LandlordSignedContractPath",
                table: "Contracts",
                newName: "FinalSignedPdfUrl");

            migrationBuilder.RenameColumn(
                name: "LandlordSignedAt",
                table: "Contracts",
                newName: "SignatureDeadline");

            migrationBuilder.RenameColumn(
                name: "IsLandlordSigned",
                table: "Contracts",
                newName: "IsOwnerSigned");

            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContractId",
                table: "EscrowTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedPdfUrl",
                table: "Contracts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OwnerSignedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "519659f7-8f35-49d0-8f11-e4c34ce36a42", "AQAAAAIAAYagAAAAEOt1rKn2JjVWhIZLXhZahdlCt7XJh2tIqWLKBIdEUVyq3nIPMDZ6KQUNjzoS6L128w==", "3f0cfdb5-c6a2-4bcc-a4bd-05921fa5aa58" });
        }
    }
}
