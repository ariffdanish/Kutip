using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kutip.Migrations
{
    /// <inheritdoc />
    public partial class fkuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Issues",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Issues",
                table: "Schedules");
        }
    }
}
