using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "004dedf6-f28f-4e57-9fa0-4593ff8b9429", "AQAAAAIAAYagAAAAEP/HrfKRvj1MYGcGHMNLLVZDfA4aWIB0Q9/3dIKlq3C5NQbZ3CHebWbkpbZqCVKWAw==", "3240bb6d-d417-4add-99f1-7b2b102cd85d" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4273711f-d28d-418c-b548-fea5e2704507", "AQAAAAIAAYagAAAAEMyRL1Kazt8InRAGx2HuW8LCRSYvBprqJhXxGgulxQuqEuYoJ9RunaZgqu+oW/fVEg==", "6ef36639-54f4-4fe1-b0c1-b97874afca39" });
        }
    }
}
