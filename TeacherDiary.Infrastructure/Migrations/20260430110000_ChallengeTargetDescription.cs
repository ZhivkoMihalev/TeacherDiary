using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TeacherDiary.Infrastructure.Persistence;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260430110000_ChallengeTargetDescription")]
    public partial class ChallengeTargetDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetDescription",
                table: "Challenges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetDescription",
                table: "Challenges");
        }
    }
}