using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUnitImageUrlToMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                values: new object[] { "83a35259-60fd-4a18-aa96-cd9d16624956", "AQAAAAIAAYagAAAAEAsTC8hx4SCUIj56bMB1eojE92QQrnyMbrp6baZ2YP+2Wnc5VhRofWzLcTJYPGhGbg==", "df915d6c-f8fd-4e48-b57d-4e9403b12fd8" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UnitImageUrl",
                table: "HousingUnits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "42cee69b-2a69-4aa2-9c6a-d59340898d79", "AQAAAAIAAYagAAAAEPv+FSwbUyBXVYjP6r8+znugb3GFBL5Xs9deb5RWTeP8b8HzlaXuhVdzfRDRrJd9Hw==", "8ef8870b-745a-48bd-8f9f-ee911ad20881" });
        }
    }
}
