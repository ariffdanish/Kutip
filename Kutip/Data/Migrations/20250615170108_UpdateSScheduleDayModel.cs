using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kutip.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSScheduleDayModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduledDateTime",
                table: "Schedules",
                newName: "ScheduledDate");

            migrationBuilder.AddColumn<int>(
                name: "ScheduledDay",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledDay",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "ScheduledDate",
                table: "Schedules",
                newName: "ScheduledDateTime");
        }
    }
}
