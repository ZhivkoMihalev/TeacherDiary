using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsToAssignedBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "AssignedBooks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "AssignedBooks");
        }
    }
}
