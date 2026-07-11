using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4273711f-d28d-418c-b548-fea5e2704507", "AQAAAAIAAYagAAAAEMyRL1Kazt8InRAGx2HuW8LCRSYvBprqJhXxGgulxQuqEuYoJ9RunaZgqu+oW/fVEg==", "6ef36639-54f4-4fe1-b0c1-b97874afca39" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "84224261-44f3-4da7-ac72-fe8706488b92", "AQAAAAIAAYagAAAAEObcBLBUMvz66BoMHmBX3HWNEX3aidneZhWPn0dZSnJClpPkUlX0OVhJg3N8D/v7vQ==", "96e297b1-ae0f-457e-8356-3bb28a0bffad" });
        }
    }
}
