using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDateDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
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
                values: new object[] { "757bd6ac-143b-45a5-a387-7ec14727b5c3", "AQAAAAIAAYagAAAAECLFCvX0b/EEj10aBvvkFaJcHPRss0ytzJ1VPQqTSUkmLIxX6pzj62sUXNSgxCEJoA==", "b9ea0ecb-cb82-4439-b36e-e4088540787e" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
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
                values: new object[] { "4b2e014f-61d5-4479-89ec-ad25a94e5f2e", "AQAAAAIAAYagAAAAEDNVXh8QuG7Rq0Qi38FN8alhSOwF1K8dnunR26+qOfNPOFiqwMHOcXr9s2TwqP6Brw==", "b6f8f493-d553-4c0f-93af-89342884e6c8" });
        }
    }
}
