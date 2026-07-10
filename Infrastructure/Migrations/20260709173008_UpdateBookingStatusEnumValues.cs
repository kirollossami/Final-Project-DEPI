using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingStatusEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update old booking status strings to new enum values
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'PendingPayment' 
                  WHERE BookingStatus = 'PaymentProcessing'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'WaitingForContract' 
                  WHERE BookingStatus = 'ContractSent'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'WaitingForSignatures' 
                  WHERE BookingStatus = 'ContractUploaded'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'WaitingForStudentSignature' 
                  WHERE BookingStatus = 'LandlordSigned'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'WaitingForLandlordSignature' 
                  WHERE BookingStatus = 'StudentSigned'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'WaitingForAdminApproval' 
                  WHERE BookingStatus = 'BothSigned'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'Approved' 
                  WHERE BookingStatus = 'Completed'");
            
            migrationBuilder.Sql(
                @"UPDATE Bookings 
                  SET BookingStatus = 'Rejected' 
                  WHERE BookingStatus = 'Declined'");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a1e07e6c-9db6-43f1-bdfa-52969310a3c5", "AQAAAAIAAYagAAAAEJq3JVLj9foYHwd/cBYb9iK/2UwHbhafOggMKwmp0cFSDz0E0r9qgpFdauvuloGk5w==", "f29969db-8b3e-4f8d-8913-8f4f438f4272" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "cf228a4d-bd4d-4087-9408-5f9627f64786", "AQAAAAIAAYagAAAAED4fV/dOTj+OYhfoG5vIfBXAO2H6/q7OJMCwB84tOddhtqT5ale2CjGTXC/13/xH5Q==", "44d5fe36-37c4-44bc-9081-b19230a24916" });
        }
    }
}
