using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizLectureId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Choice = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_answers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_answers_UserId_QuizLectureId",
                table: "user_answers",
                columns: new[] { "UserId", "QuizLectureId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_answers_UserId_QuizLectureId_QuestionId_OptionId",
                table: "user_answers",
                columns: new[] { "UserId", "QuizLectureId", "QuestionId", "OptionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_answers");
        }
    }
}
