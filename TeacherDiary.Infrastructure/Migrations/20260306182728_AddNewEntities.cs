using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "AssignmentProgress",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "AssignmentProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferenceType",
                table: "ActivityLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalPoints = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentPoints_Students_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentStreaks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
                    BestStreak = table.Column<int>(type: "int", nullable: false),
                    LastActiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentStreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentStreaks_Students_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentBadges_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentBadges_Students_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActivityType",
                table: "ActivityLogs",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Date",
                table: "ActivityLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_StudentProfileId",
                table: "ActivityLogs",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadges_BadgeId",
                table: "StudentBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadges_StudentProfileId_BadgeId",
                table: "StudentBadges",
                columns: new[] { "StudentProfileId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPoints_StudentProfileId",
                table: "StudentPoints",
                column: "StudentProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentStreaks_StudentProfileId",
                table: "StudentStreaks",
                column: "StudentProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentBadges");

            migrationBuilder.DropTable(
                name: "StudentPoints");

            migrationBuilder.DropTable(
                name: "StudentStreaks");

            migrationBuilder.DropTable(
                name: "Badges");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_ActivityType",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_Date",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_StudentProfileId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "AssignmentProgress");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "AssignmentProgress");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "ActivityLogs");
        }
    }
}
