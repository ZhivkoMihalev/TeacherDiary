using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsToAssignmentsAndChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Challenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Assignments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Assignments");
        }
    }
}
