using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleLoginFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnboardingComplete",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoogleUser",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "IsOnboardingComplete",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsGoogleUser",
                table: "AspNetUsers");
        }
    }
}
