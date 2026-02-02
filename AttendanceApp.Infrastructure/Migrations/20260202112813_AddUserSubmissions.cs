using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizLectureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Submitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_submissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_submissions_UserId_QuizLectureId",
                table: "user_submissions",
                columns: new[] { "UserId", "QuizLectureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_submissions");
        }
    }
}
