using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kutip.Migrations
{
    /// <inheritdoc />
    public partial class FixScheduleDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupEvents_Schedules_RelatedScheduleId",
                table: "PickupEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupEvents_Schedules_RelatedScheduleId",
                table: "PickupEvents",
                column: "RelatedScheduleId",
                principalTable: "Schedules",
                principalColumn: "ScheduleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PickupEvents_Schedules_RelatedScheduleId",
                table: "PickupEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_PickupEvents_Schedules_RelatedScheduleId",
                table: "PickupEvents",
                column: "RelatedScheduleId",
                principalTable: "Schedules",
                principalColumn: "ScheduleId");
        }
    }
}
