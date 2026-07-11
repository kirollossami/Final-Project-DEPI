using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingToWaitingForContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix old 'Pending' booking status to 'PendingPayment' (old enum name before refactoring)
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'PendingPayment' 
                  WHERE BookingStatus = 'Pending'");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e761d59b-ce87-4326-bccb-385ad5563c45", "AQAAAAIAAYagAAAAEDYXmBGLnSHi8g5QOn3gFh7Vde4jXW71BeZwNjGKYeg/Eg/6Uj2TMfgHZc9k9dK7ow==", "c03e1cd3-56f7-4b9d-9630-1f15d0d9d78d" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a1e07e6c-9db6-43f1-bdfa-52969310a3c5", "AQAAAAIAAYagAAAAEJq3JVLj9foYHwd/cBYb9iK/2UwHbhafOggMKwmp0cFSDz0E0r9qgpFdauvuloGk5w==", "f29969db-8b3e-4f8d-8913-8f4f438f4272" });
        }
    }
}
