using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePaymentFieldsAndBookingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b4ed0518-9310-41a6-8f07-f5ceecf6d91b", "AQAAAAIAAYagAAAAEHEQ6uwWDcfl6xxljbETHHwieuusgEBmxHBtF7/BjMycBUr8L4U8z2KdpSRdIWks8Q==", "748665fa-15ca-4399-baae-63e1b1b33d92" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "824197b3-393f-498e-ac1e-87149880f30c", "AQAAAAIAAYagAAAAEP1EW/ahq30NGp6Zv+nH2JlB1R25wmydyJDu5hDIFxWueEYcs64KU0eHaOfdTZ3OIQ==", "0ee8af06-ac3b-499d-9e63-2ee2f7c0fc53" });
        }
    }
}
