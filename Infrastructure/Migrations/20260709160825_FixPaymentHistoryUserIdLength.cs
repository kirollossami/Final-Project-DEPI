using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentHistoryUserIdLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentHistories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "cf228a4d-bd4d-4087-9408-5f9627f64786", "AQAAAAIAAYagAAAAED4fV/dOTj+OYhfoG5vIfBXAO2H6/q7OJMCwB84tOddhtqT5ale2CjGTXC/13/xH5Q==", "44d5fe36-37c4-44bc-9081-b19230a24916" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PaymentHistories",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "6ec522b6-9667-42e5-8865-65f5e50dd61b", "AQAAAAIAAYagAAAAEH+EcBYy8C+VuPEELzhqHNLTSyeFcXKTI8g8o37sHRlNrFTpSWxkMCmMgGZfGZxJOg==", "ceb1ec6e-c735-47f8-b5a4-98b7d1691904" });
        }
    }
}
