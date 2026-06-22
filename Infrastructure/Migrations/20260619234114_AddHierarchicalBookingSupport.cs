using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchicalBookingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Rooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Rooms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseMonthlyPrice",
                table: "HousingUnits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "HousingUnits",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HousingUnits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoomId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "BedId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingType",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "HousingUnitId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    BedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BedNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.BedId);
                    table.ForeignKey(
                        name: "FK_Beds_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f55e7a6f-982d-446a-aba0-bedc7fb3ca2d", "AQAAAAIAAYagAAAAECDbhemZoX7ynk6BZCJTkhFr7wvinLCNjtuyhpCUywHv/OaGdQay8fGYQNaYHE4Eyg==", "c0f40086-dded-483f-a25c-f58deb2eab14" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BedId",
                table: "Bookings",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HousingUnitId",
                table: "Bookings",
                column: "HousingUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_RoomId",
                table: "Beds",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Beds_BedId",
                table: "Bookings",
                column: "BedId",
                principalTable: "Beds",
                principalColumn: "BedId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_HousingUnits_HousingUnitId",
                table: "Bookings",
                column: "HousingUnitId",
                principalTable: "HousingUnits",
                principalColumn: "HousingUnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Beds_BedId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_HousingUnits_HousingUnitId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BedId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_HousingUnitId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "BaseMonthlyPrice",
                table: "HousingUnits");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HousingUnits");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HousingUnits");

            migrationBuilder.DropColumn(
                name: "BedId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingType",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "HousingUnitId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoomId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9ef14dda-cd62-4b7d-a81f-904dc3268d87", "AQAAAAIAAYagAAAAEFFqrN0wfsWgnt5YMkDgOnl96uRYkHUtqWMfQc959A3Y41Nn7lPgchgnzqjlhQ6IOg==", "c651695f-12aa-4fca-a9ed-7b9092f278fb" });
        }
    }
}
