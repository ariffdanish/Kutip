using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kutip.Data.Migrations
{
    /// <inheritdoc />
    public partial class SayaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "Trucks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "Trucks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Bin",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Bin",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TruckId",
                table: "Bin",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bin_TruckId",
                table: "Bin",
                column: "TruckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bin_Trucks_TruckId",
                table: "Bin",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "TruckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bin_Trucks_TruckId",
                table: "Bin");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Bin_TruckId",
                table: "Bin");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Bin");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Bin");

            migrationBuilder.DropColumn(
                name: "TruckId",
                table: "Bin");
        }
    }
}
