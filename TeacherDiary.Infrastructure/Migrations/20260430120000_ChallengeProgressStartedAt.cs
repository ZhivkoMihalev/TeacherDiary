using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TeacherDiary.Infrastructure.Persistence;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260430120000_ChallengeProgressStartedAt")]
    public partial class ChallengeProgressStartedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ChallengeProgress",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ChallengeProgress");
        }
    }
}
