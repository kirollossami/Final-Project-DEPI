using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationsAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "824197b3-393f-498e-ac1e-87149880f30c", "AQAAAAIAAYagAAAAEP1EW/ahq30NGp6Zv+nH2JlB1R25wmydyJDu5hDIFxWueEYcs64KU0eHaOfdTZ3OIQ==", "0ee8af06-ac3b-499d-9e63-2ee2f7c0fc53" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7bcc41a6-f248-479d-8e93-fd8249b12eb3", "AQAAAAIAAYagAAAAEEb3Oq5X/71DetOKpGq9JsJoJ1OkIgrJwjP1vaWb07vVM4cjkbXvuTnpxDHe4t9lPQ==", "7ad24c00-db39-41ab-bebf-db1cb1f6fa85" });
        }
    }
}
