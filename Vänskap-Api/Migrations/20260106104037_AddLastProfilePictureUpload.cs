using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vänskap_Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLastProfilePictureUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageUpdateCountToday",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastImageUpdate",
                table: "Events",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProfilePictureUpload",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfilePictureUploadCountToday",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUpdateCountToday",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LastImageUpdate",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LastProfilePictureUpload",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUploadCountToday",
                table: "AspNetUsers");
        }
    }
}
