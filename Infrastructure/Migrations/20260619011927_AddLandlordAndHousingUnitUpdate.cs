using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLandlordAndHousingUnitUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_LandLords_LandLordId",
                table: "Complaints");

            migrationBuilder.RenameColumn(
                name: "LandLordId",
                table: "Complaints",
                newName: "HousingUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Complaints_LandLordId",
                table: "Complaints",
                newName: "IX_Complaints_HousingUnitId");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentOccupancy",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerMonth",
                table: "Rooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "LandLords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "UnitImageUrl",
                table: "HousingUnits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "HousingUnits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractId",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractPdfUrl",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnitImages",
                columns: table => new
                {
                    UnitImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HousingUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitImages", x => x.UnitImageId);
                    table.ForeignKey(
                        name: "FK_UnitImages_HousingUnits_HousingUnitId",
                        column: x => x.HousingUnitId,
                        principalTable: "HousingUnits",
                        principalColumn: "HousingUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9ef14dda-cd62-4b7d-a81f-904dc3268d87", "AQAAAAIAAYagAAAAEFFqrN0wfsWgnt5YMkDgOnl96uRYkHUtqWMfQc959A3Y41Nn7lPgchgnzqjlhQ6IOg==", "c651695f-12aa-4fca-a9ed-7b9092f278fb" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitImages_HousingUnitId",
                table: "UnitImages",
                column: "HousingUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_HousingUnits_HousingUnitId",
                table: "Complaints",
                column: "HousingUnitId",
                principalTable: "HousingUnits",
                principalColumn: "HousingUnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_HousingUnits_HousingUnitId",
                table: "Complaints");

            migrationBuilder.DropTable(
                name: "UnitImages");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CurrentOccupancy",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "PricePerMonth",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "LandLords");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "HousingUnits");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ContractPdfUrl",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "HousingUnitId",
                table: "Complaints",
                newName: "LandLordId");

            migrationBuilder.RenameIndex(
                name: "IX_Complaints_HousingUnitId",
                table: "Complaints",
                newName: "IX_Complaints_LandLordId");

            migrationBuilder.AlterColumn<string>(
                name: "UnitImageUrl",
                table: "HousingUnits",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b7ddcc98-739c-4603-8036-9ada26897501", "AQAAAAIAAYagAAAAEOPkfFC9o7HdqTZXFHQPMZT7dhYmogH+V5cG0rbND+QklAb5/AYDr5+QdwOu7o+w0w==", "1e1faff9-0f47-40a1-95c1-765462e1df3a" });

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_LandLords_LandLordId",
                table: "Complaints",
                column: "LandLordId",
                principalTable: "LandLords",
                principalColumn: "LandLordId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
