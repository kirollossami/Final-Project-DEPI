using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientSecretToPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "PaymentTransactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "84224261-44f3-4da7-ac72-fe8706488b92", "AQAAAAIAAYagAAAAEObcBLBUMvz66BoMHmBX3HWNEX3aidneZhWPn0dZSnJClpPkUlX0OVhJg3N8D/v7vQ==", "96e297b1-ae0f-457e-8356-3bb28a0bffad" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "PaymentTransactions");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "757bd6ac-143b-45a5-a387-7ec14727b5c3", "AQAAAAIAAYagAAAAECLFCvX0b/EEj10aBvvkFaJcHPRss0ytzJ1VPQqTSUkmLIxX6pzj62sUXNSgxCEJoA==", "b9ea0ecb-cb82-4439-b36e-e4088540787e" });
        }
    }
}
