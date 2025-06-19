using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kutip.Migrations
{
    /// <inheritdoc />
    public partial class penat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupEvents_Bin_BinId",
                table: "PickupEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupEvents_Trucks_TruckId",
                table: "PickupEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Bin_BinId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Trucks_TruckId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_PickupEvents_BinId",
                table: "PickupEvents");

            migrationBuilder.DropIndex(
                name: "IX_PickupEvents_TruckId",
                table: "PickupEvents");

            migrationBuilder.DropColumn(
                name: "BinId",
                table: "PickupEvents");

            migrationBuilder.DropColumn(
                name: "TruckId",
                table: "PickupEvents");

            migrationBuilder.AddColumn<int>(
                name: "BinId1",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TruckId1",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_BinId1",
                table: "Schedules",
                column: "BinId1");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TruckId1",
                table: "Schedules",
                column: "TruckId1");

            migrationBuilder.CreateIndex(
                name: "IX_PickupEvents_RelatedBinId",
                table: "PickupEvents",
                column: "RelatedBinId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupEvents_RelatedTruckId",
                table: "PickupEvents",
                column: "RelatedTruckId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupEvents_Bin_RelatedBinId",
                table: "PickupEvents",
                column: "RelatedBinId",
                principalTable: "Bin",
                principalColumn: "BinId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PickupEvents_Trucks_RelatedTruckId",
                table: "PickupEvents",
                column: "RelatedTruckId",
                principalTable: "Trucks",
                principalColumn: "TruckId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Bin_BinId",
                table: "Schedules",
                column: "BinId",
                principalTable: "Bin",
                principalColumn: "BinId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Bin_BinId1",
                table: "Schedules",
                column: "BinId1",
                principalTable: "Bin",
                principalColumn: "BinId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Trucks_TruckId",
                table: "Schedules",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "TruckId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Trucks_TruckId1",
                table: "Schedules",
                column: "TruckId1",
                principalTable: "Trucks",
                principalColumn: "TruckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupEvents_Bin_RelatedBinId",
                table: "PickupEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_PickupEvents_Trucks_RelatedTruckId",
                table: "PickupEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Bin_BinId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Bin_BinId1",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Trucks_TruckId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Trucks_TruckId1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_BinId1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TruckId1",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_PickupEvents_RelatedBinId",
                table: "PickupEvents");

            migrationBuilder.DropIndex(
                name: "IX_PickupEvents_RelatedTruckId",
                table: "PickupEvents");

            migrationBuilder.DropColumn(
                name: "BinId1",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "TruckId1",
                table: "Schedules");

            migrationBuilder.AddColumn<int>(
                name: "BinId",
                table: "PickupEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TruckId",
                table: "PickupEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickupEvents_BinId",
                table: "PickupEvents",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupEvents_TruckId",
                table: "PickupEvents",
                column: "TruckId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupEvents_Bin_BinId",
                table: "PickupEvents",
                column: "BinId",
                principalTable: "Bin",
                principalColumn: "BinId");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupEvents_Trucks_TruckId",
                table: "PickupEvents",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "TruckId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Bin_BinId",
                table: "Schedules",
                column: "BinId",
                principalTable: "Bin",
                principalColumn: "BinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Trucks_TruckId",
                table: "Schedules",
                column: "TruckId",
                principalTable: "Trucks",
                principalColumn: "TruckId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
