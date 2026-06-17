using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversityVerificationAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacultyName",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityEmail",
                table: "Students",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityIdCardPath",
                table: "Students",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityName",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityVerificationStatus",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotSubmitted");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Complaints",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionRecords",
                columns: table => new
                {
                    CommissionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRecords", x => x.CommissionRecordId);
                    table.ForeignKey(
                        name: "FK_CommissionRecords_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Restrict);
                });

            // Remap ComplaintStatus values from old enum to new
            migrationBuilder.Sql("UPDATE Complaints SET Status = 'Open' WHERE Status = 'Pending'");
            migrationBuilder.Sql("UPDATE Complaints SET Status = 'InInvestigation' WHERE Status = 'InProgress'");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "GoogleId", "IsActive", "IsDeleted", "IsGoogleUser", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfileImage", "SecurityStamp", "TwoFactorEnabled", "TwoFactorSecret", "UserName" },
                values: new object[] { "admin-user-id-001", 0, "b7ddcc98-739c-4603-8036-9ada26897501", "admin@studenthousing.com", true, null, true, false, false, false, null, "ADMIN@STUDENTHOUSING.COM", "ADMIN@STUDENTHOUSING.COM", "AQAAAAIAAYagAAAAEOPkfFC9o7HdqTZXFHQPMZT7dhYmogH+V5cG0rbND+QklAb5/AYDr5+QdwOu7o+w0w==", null, false, null, "1e1faff9-0f47-40a1-95c1-765462e1df3a", false, null, "admin@studenthousing.com" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "1", "admin-user-id-001" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_BookingId",
                table: "CommissionRecords",
                column: "BookingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse ComplaintStatus remap
            migrationBuilder.Sql("UPDATE Complaints SET Status = 'Pending' WHERE Status = 'Open'");
            migrationBuilder.Sql("UPDATE Complaints SET Status = 'InProgress' WHERE Status = 'InInvestigation'");

            migrationBuilder.DropTable(
                name: "CommissionRecords");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "1", "admin-user-id-001" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001");

            migrationBuilder.DropColumn(
                name: "FacultyName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UniversityEmail",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UniversityIdCardPath",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UniversityName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UniversityVerificationStatus",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Complaints");
        }
    }
}
