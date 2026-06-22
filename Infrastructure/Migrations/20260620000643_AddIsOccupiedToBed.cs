using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOccupiedToBed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOccupied",
                table: "Beds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "013b7be7-470b-4572-848b-40e67a7e9a9e", "AQAAAAIAAYagAAAAENGwDqamJDfdgud9HisfW4Fxpwq7dPVDdy6gA83kQppj0aivgzekB5mx+eQr4FaGvQ==", "85b21b4e-b16f-4235-8eac-6301e5341d24" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOccupied",
                table: "Beds");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f55e7a6f-982d-446a-aba0-bedc7fb3ca2d", "AQAAAAIAAYagAAAAECDbhemZoX7ynk6BZCJTkhFr7wvinLCNjtuyhpCUywHv/OaGdQay8fGYQNaYHE4Eyg==", "c0f40086-dded-483f-a25c-f58deb2eab14" });
        }
    }
}
