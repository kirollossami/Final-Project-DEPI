using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionCreatedAtDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4b2e014f-61d5-4479-89ec-ad25a94e5f2e", "AQAAAAIAAYagAAAAEDNVXh8QuG7Rq0Qi38FN8alhSOwF1K8dnunR26+qOfNPOFiqwMHOcXr9s2TwqP6Brw==", "b6f8f493-d553-4c0f-93af-89342884e6c8" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentTransactions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d0d3e149-dc9a-41a2-9257-9ae80229e7f9", "AQAAAAIAAYagAAAAEOtQBgdDegqmQOVHw0Cui1IROJVfJPr3RClNcYCzyzac+oZH0JlrTsVlwY/IcQoJDQ==", "b463da22-a985-4310-bfab-301463db1cc0" });
        }
    }
}
