using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreBookingConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_BookingId",
                table: "Conversations");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "HousingUnitId",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "admin-user-id-001",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "739eb7e6-344d-4f52-a7e7-ae5bbd22dc24", "AQAAAAIAAYagAAAAEMNCgvKl5cgfSoXngzQQVt2QWjfFa7EFLDO3UfIpoK5qUa+ZZoku9pLtWyqTyen07A==", "e3ed5410-3299-4705-8f2f-fd94942aaf52" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BookingId",
                table: "Conversations",
                column: "BookingId",
                unique: true,
                filter: "[BookingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_HousingUnitId",
                table: "Conversations",
                column: "HousingUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_HousingUnits_HousingUnitId",
                table: "Conversations",
                column: "HousingUnitId",
                principalTable: "HousingUnits",
                principalColumn: "HousingUnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_HousingUnits_HousingUnitId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_BookingId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_HousingUnitId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "HousingUnitId",
                table: "Conversations");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "Conversations",
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
                values: new object[] { "b4ed0518-9310-41a6-8f07-f5ceecf6d91b", "AQAAAAIAAYagAAAAEHEQ6uwWDcfl6xxljbETHHwieuusgEBmxHBtF7/BjMycBUr8L4U8z2KdpSRdIWks8Q==", "748665fa-15ca-4399-baae-63e1b1b33d92" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_BookingId",
                table: "Conversations",
                column: "BookingId",
                unique: true);
        }
    }
}
