using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherDiary.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningActivityEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassRoomId",
                table: "Challenges");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StudentStreaks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentStreaks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StudentStreaks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedActivitiesCount",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Students",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Students",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StudentPoints",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentPoints",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StudentPoints",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StudentBadges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentBadges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StudentBadges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ReadingProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ReadingProgress",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ReadingProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Organizations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Organizations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Classes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Classes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Classes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassId",
                table: "Challenges",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Challenges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Challenges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Challenges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ChallengeProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChallengeProgress",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ChallengeProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Books",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Books",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalPages",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Books",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Badges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Badges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Badges",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssignmentProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssignmentProgress",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AssignmentProgress",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AssignedBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssignedBooks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AssignedBooks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ActivityLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ActivityLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PointsEarned",
                table: "ActivityLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ActivityLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LearningActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetValue = table.Column<int>(type: "int", nullable: true),
                    MaxScore = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningActivities_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentLearningActivityProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentValue = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<int>(type: "int", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLearningActivityProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentLearningActivityProgress_LearningActivities_LearningActivityId",
                        column: x => x.LearningActivityId,
                        principalTable: "LearningActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentLearningActivityProgress_Students_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_AssignedBookId",
                table: "LearningActivities",
                column: "AssignedBookId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_AssignmentId",
                table: "LearningActivities",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_ChallengeId",
                table: "LearningActivities",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_ClassId_Type",
                table: "LearningActivities",
                columns: new[] { "ClassId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLearningActivityProgress_LearningActivityId",
                table: "StudentLearningActivityProgress",
                column: "LearningActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLearningActivityProgress_StudentProfileId_LearningActivityId",
                table: "StudentLearningActivityProgress",
                columns: new[] { "StudentProfileId", "LearningActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentLearningActivityProgress_StudentProfileId_Status",
                table: "StudentLearningActivityProgress",
                columns: new[] { "StudentProfileId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentLearningActivityProgress");

            migrationBuilder.DropTable(
                name: "LearningActivities");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StudentStreaks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudentStreaks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentStreaks");

            migrationBuilder.DropColumn(
                name: "CompletedActivitiesCount",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StudentPoints");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudentPoints");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentPoints");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StudentBadges");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudentBadges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentBadges");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ReadingProgress");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ReadingProgress");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ReadingProgress");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChallengeProgress");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChallengeProgress");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ChallengeProgress");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "TotalPages",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssignmentProgress");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssignmentProgress");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssignmentProgress");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AssignedBooks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssignedBooks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssignedBooks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ActivityLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClassId",
                table: "Challenges",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassRoomId",
                table: "Challenges",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
