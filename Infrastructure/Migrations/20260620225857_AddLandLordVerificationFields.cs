using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLandLordVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LandLords",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "HousingUnitDocumentationUrl",
                table: "LandLords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalIdImageUrl",
                table: "LandLords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "LandLords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "484339af-7b09-49e5-9504-d5e80896fde3", "AQAAAAIAAYagAAAAEH3fcC04/lzteoyKTBKqZv9Q3/ZBe54vONX1FO/ZlQn8QU9snNgBj6iVx44bI8iMpg==", "2ab862e3-63c3-4397-8453-9f09d2c4ae2b" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LandLords");

            migrationBuilder.DropColumn(
                name: "HousingUnitDocumentationUrl",
                table: "LandLords");

            migrationBuilder.DropColumn(
                name: "NationalIdImageUrl",
                table: "LandLords");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "LandLords");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "013b7be7-470b-4572-848b-40e67a7e9a9e", "AQAAAAIAAYagAAAAENGwDqamJDfdgud9HisfW4Fxpwq7dPVDdy6gA83kQppj0aivgzekB5mx+eQr4FaGvQ==", "85b21b4e-b16f-4235-8eac-6301e5341d24" });
        }
    }
}
