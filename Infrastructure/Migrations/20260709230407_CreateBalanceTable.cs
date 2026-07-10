using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateBalanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "aaf8eb86-2e96-49f7-94e3-6c9b39a476df", "AQAAAAIAAYagAAAAEN/iXtf56uJXOjF3RsfJbvQA0lt/GorNxnvfBAsWRKkXaiaaMRXTViWfJ0oYnx7a4g==", "773c36da-29b8-417d-8063-97eba5110af1" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "004dedf6-f28f-4e57-9fa0-4593ff8b9429", "AQAAAAIAAYagAAAAEP/HrfKRvj1MYGcGHMNLLVZDfA4aWIB0Q9/3dIKlq3C5NQbZ3CHebWbkpbZqCVKWAw==", "3240bb6d-d417-4add-99f1-7b2b102cd85d" });
        }
    }
}
