using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "lectures",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocationAtJoin",
                table: "lecture_attendees",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "lectures");

            migrationBuilder.DropColumn(
                name: "LocationAtJoin",
                table: "lecture_attendees");
        }
    }
}
